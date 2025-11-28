using Olbrasoft.VoiceAssistant.ContinuousListener.Services;

namespace Olbrasoft.VoiceAssistant.ContinuousListener;

/// <summary>
/// Main worker that continuously listens for speech, transcribes it, 
/// and detects wake words to dispatch commands.
/// </summary>
public class ContinuousListenerWorker : BackgroundService
{
    private readonly ILogger<ContinuousListenerWorker> _logger;
    private readonly AudioCaptureService _audioCapture;
    private readonly VadService _vad;
    private readonly TranscriptionService _transcription;
    private readonly WakeWordService _wakeWord;
    private readonly CommandDispatcher _dispatcher;
    private readonly ContinuousListenerOptions _options;

    // State machine
    private enum State { Waiting, Recording }
    private State _state = State.Waiting;

    // Buffers
    private readonly Queue<byte[]> _preBuffer = new();
    private readonly List<byte[]> _speechBuffer = new();
    private int _preBufferBytes = 0;
    private int _speechBufferBytes = 0;

    // Timing
    private DateTime _silenceStart;
    private DateTime _recordingStart;
    private int _segmentCount = 0;

    public ContinuousListenerWorker(
        ILogger<ContinuousListenerWorker> logger,
        IConfiguration configuration,
        AudioCaptureService audioCapture,
        VadService vad,
        TranscriptionService transcription,
        WakeWordService wakeWord,
        CommandDispatcher dispatcher)
    {
        _logger = logger;
        _audioCapture = audioCapture;
        _vad = vad;
        _transcription = transcription;
        _wakeWord = wakeWord;
        _dispatcher = dispatcher;

        _options = new ContinuousListenerOptions();
        configuration.GetSection(ContinuousListenerOptions.SectionName).Bind(_options);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("═══════════════════════════════════════════════════════════════");
        _logger.LogInformation("  ContinuousListener Starting");
        _logger.LogInformation("═══════════════════════════════════════════════════════════════");
        _logger.LogInformation("  VAD chunk: {VadChunkMs}ms ({ChunkSize} bytes)", 
            _options.VadChunkMs, _options.ChunkSizeBytes);
        _logger.LogInformation("  Pre-buffer: {PreBufferMs}ms", _options.PreBufferMs);
        _logger.LogInformation("  Post-silence: {PostSilenceMs}ms", _options.PostSilenceMs);
        _logger.LogInformation("  Min recording: {MinRecordingMs}ms", _options.MinRecordingMs);
        _logger.LogInformation("  Silence threshold: {Threshold} RMS", _options.SilenceThreshold);
        _logger.LogInformation("  Wake words: {WakeWords}", string.Join(", ", _options.WakeWords));
        _logger.LogInformation("═══════════════════════════════════════════════════════════════");

        // Initialize services
        try
        {
            _transcription.Initialize();
            _audioCapture.Start();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize services");
            return;
        }

        _logger.LogInformation("Listening... (State: WAITING)");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var chunk = await _audioCapture.ReadChunkAsync(stoppingToken);
                if (chunk == null) break;

                await ProcessChunkAsync(chunk, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in main loop");
        }
        finally
        {
            _audioCapture.Stop();
            _logger.LogInformation("ContinuousListener stopped");
        }
    }

    private async Task ProcessChunkAsync(byte[] chunk, CancellationToken cancellationToken)
    {
        var (isSpeech, rms) = _vad.Analyze(chunk);

        switch (_state)
        {
            case State.Waiting:
                if (isSpeech)
                {
                    TransitionToRecording(rms);
                    
                    // Move pre-buffer to speech buffer
                    while (_preBuffer.Count > 0)
                    {
                        var c = _preBuffer.Dequeue();
                        _speechBuffer.Add(c);
                        _speechBufferBytes += c.Length;
                    }
                    _preBufferBytes = 0;

                    // Add current chunk
                    _speechBuffer.Add(chunk);
                    _speechBufferBytes += chunk.Length;
                }
                else
                {
                    // Keep in pre-buffer
                    _preBuffer.Enqueue(chunk);
                    _preBufferBytes += chunk.Length;

                    // Trim if too large
                    while (_preBufferBytes > _options.PreBufferMaxBytes && _preBuffer.Count > 0)
                    {
                        var removed = _preBuffer.Dequeue();
                        _preBufferBytes -= removed.Length;
                    }
                }
                break;

            case State.Recording:
                // Always add to speech buffer
                _speechBuffer.Add(chunk);
                _speechBufferBytes += chunk.Length;

                if (isSpeech)
                {
                    // Reset silence timer
                    _silenceStart = default;
                }
                else
                {
                    // Start or continue silence timer
                    if (_silenceStart == default)
                    {
                        _silenceStart = DateTime.UtcNow;
                    }

                    var silenceMs = (DateTime.UtcNow - _silenceStart).TotalMilliseconds;
                    var recordingMs = (DateTime.UtcNow - _recordingStart).TotalMilliseconds;

                    // Check if we should complete
                    if (silenceMs >= _options.PostSilenceMs)
                    {
                        if (recordingMs >= _options.MinRecordingMs)
                        {
                            await CompleteRecordingAsync(cancellationToken);
                        }
                        else
                        {
                            _logger.LogDebug("Recording too short ({RecordingMs}ms < {MinMs}ms), discarding", 
                                recordingMs, _options.MinRecordingMs);
                            ResetToWaiting();
                        }
                    }
                }
                break;
        }
    }

    private void TransitionToRecording(float rms)
    {
        _state = State.Recording;
        _recordingStart = DateTime.UtcNow;
        _silenceStart = default;

        _logger.LogInformation("🎤 SPEECH DETECTED (RMS={Rms:F4}), WAITING → RECORDING", rms);
    }

    private async Task CompleteRecordingAsync(CancellationToken cancellationToken)
    {
        _segmentCount++;
        var duration = DateTime.UtcNow - _recordingStart;

        _logger.LogInformation("═══════════════════════════════════════════════════════════════");
        _logger.LogInformation("  SEGMENT #{Count} COMPLETE", _segmentCount);
        _logger.LogInformation("  Duration: {Duration}ms, Audio: {Bytes} bytes", 
            duration.TotalMilliseconds, _speechBufferBytes);
        _logger.LogInformation("═══════════════════════════════════════════════════════════════");

        // Combine all chunks
        var audioData = new byte[_speechBufferBytes];
        int offset = 0;
        foreach (var chunk in _speechBuffer)
        {
            Buffer.BlockCopy(chunk, 0, audioData, offset, chunk.Length);
            offset += chunk.Length;
        }

        // Transcribe
        try
        {
            _logger.LogInformation("Transcribing...");
            var result = await _transcription.TranscribeAsync(audioData);

            if (result.Success && !string.IsNullOrWhiteSpace(result.Text))
            {
                _logger.LogInformation("Transcript: \"{Text}\"", result.Text);
                
                // Detect wake word
                var wakeResult = _wakeWord.Detect(result.Text);
                
                if (wakeResult.Detected)
                {
                    _logger.LogInformation("🎯 WAKE WORD: \"{WakeWord}\"", wakeResult.WakeWord);
                    
                    if (!string.IsNullOrWhiteSpace(wakeResult.Command))
                    {
                        _logger.LogInformation("📤 COMMAND: \"{Command}\"", wakeResult.Command);
                        await _dispatcher.DispatchAsync(wakeResult.Command, submitPrompt: true, cancellationToken);
                    }
                    else
                    {
                        _logger.LogInformation("Wake word detected, but no command follows");
                    }
                }
            }
            else
            {
                _logger.LogDebug("No speech detected or transcription error");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transcription failed");
        }

        ResetToWaiting();
    }

    private void ResetToWaiting()
    {
        _state = State.Waiting;
        _speechBuffer.Clear();
        _speechBufferBytes = 0;
        _silenceStart = default;

        _logger.LogInformation("→ WAITING");
    }
}
