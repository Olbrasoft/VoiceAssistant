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
    private readonly WakeWordResponseService _wakeWordResponse;
    private readonly TtsControlService _ttsControl;
    private readonly SpeechLockService _speechLock;
    private readonly ContinuousListenerOptions _options;

    // State machine
    private enum State { Waiting, Recording, CollectingOpenCodeCommand }
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

    // OpenCode command collection
    private readonly List<string> _openCodeCommandParts = new();
    private DateTime _openCodeCollectionStart;

    public ContinuousListenerWorker(
        ILogger<ContinuousListenerWorker> logger,
        IConfiguration configuration,
        AudioCaptureService audioCapture,
        VadService vad,
        TranscriptionService transcription,
        WakeWordService wakeWord,
        CommandDispatcher dispatcher,
        WakeWordResponseService wakeWordResponse,
        TtsControlService ttsControl,
        SpeechLockService speechLock)
    {
        _logger = logger;
        _audioCapture = audioCapture;
        _vad = vad;
        _transcription = transcription;
        _wakeWord = wakeWord;
        _dispatcher = dispatcher;
        _wakeWordResponse = wakeWordResponse;
        _ttsControl = ttsControl;
        _speechLock = speechLock;

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
        _logger.LogInformation("  OpenCode post-silence: {OpenCodePostSilenceMs}ms", _options.OpenCodePostSilenceMs);
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

            case State.CollectingOpenCodeCommand:
                // In this state, we're collecting command segments after OpenCode wake word
                if (isSpeech)
                {
                    // Start recording if not already
                    if (_speechBuffer.Count == 0)
                    {
                        _recordingStart = DateTime.UtcNow;
                    }
                    
                    _speechBuffer.Add(chunk);
                    _speechBufferBytes += chunk.Length;
                    _silenceStart = default;
                }
                else
                {
                    if (_speechBuffer.Count > 0)
                    {
                        _speechBuffer.Add(chunk);
                        _speechBufferBytes += chunk.Length;
                    }

                    // Start or continue silence timer
                    if (_silenceStart == default)
                    {
                        _silenceStart = DateTime.UtcNow;
                    }

                    var silenceMs = (DateTime.UtcNow - _silenceStart).TotalMilliseconds;

                    // Use longer timeout for OpenCode command collection
                    if (silenceMs >= _options.OpenCodePostSilenceMs)
                    {
                        // Process any remaining audio
                        if (_speechBufferBytes > 0)
                        {
                            await ProcessOpenCodeSegmentAsync(cancellationToken);
                        }
                        
                        // Send collected command to OpenCode
                        await DispatchOpenCodeCommandAsync(cancellationToken);
                        ResetToWaiting();
                    }
                    else if (_speechBufferBytes > 0 && silenceMs >= _options.PostSilenceMs)
                    {
                        // Process this segment but keep collecting
                        await ProcessOpenCodeSegmentAsync(cancellationToken);
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

    private void TransitionToCollectingOpenCodeCommand()
    {
        _state = State.CollectingOpenCodeCommand;
        _openCodeCommandParts.Clear();
        _openCodeCollectionStart = DateTime.UtcNow;
        _speechBuffer.Clear();
        _speechBufferBytes = 0;
        _silenceStart = DateTime.UtcNow; // Start silence timer immediately

        _logger.LogInformation("🎧 RECORDING → COLLECTING OPENCODE COMMAND");
    }

    private async Task ProcessOpenCodeSegmentAsync(CancellationToken cancellationToken)
    {
        if (_speechBufferBytes == 0) return;

        _segmentCount++;
        _logger.LogInformation("  Processing OpenCode segment #{Count} ({Bytes} bytes)", 
            _segmentCount, _speechBufferBytes);

        // Combine all chunks
        var audioData = new byte[_speechBufferBytes];
        int offset = 0;
        foreach (var chunk in _speechBuffer)
        {
            Buffer.BlockCopy(chunk, 0, audioData, offset, chunk.Length);
            offset += chunk.Length;
        }

        // Clear buffer for next segment
        _speechBuffer.Clear();
        _speechBufferBytes = 0;

        // Transcribe
        try
        {
            var result = await _transcription.TranscribeAsync(audioData);
            if (result.Success && !string.IsNullOrWhiteSpace(result.Text))
            {
                _logger.LogInformation("  Segment text: \"{Text}\"", result.Text);
                _openCodeCommandParts.Add(result.Text);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transcription failed for OpenCode segment");
        }
    }

    private async Task DispatchOpenCodeCommandAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_openCodeCommandParts.Count == 0)
            {
                _logger.LogInformation("No command collected after OpenCode wake word");
                return;
            }

            var fullCommand = string.Join(" ", _openCodeCommandParts);
            var collectionDuration = DateTime.UtcNow - _openCodeCollectionStart;

            _logger.LogInformation("═══════════════════════════════════════════════════════════════");
            _logger.LogInformation("  📤 DISPATCHING TO OPENCODE");
            _logger.LogInformation("  Command: \"{Command}\"", fullCommand);
            _logger.LogInformation("  Collection time: {Duration}ms, Segments: {Count}", 
                collectionDuration.TotalMilliseconds, _openCodeCommandParts.Count);
            _logger.LogInformation("═══════════════════════════════════════════════════════════════");

            await _dispatcher.DispatchAsync(fullCommand, submitPrompt: true, cancellationToken);
        }
        finally
        {
            // Always release the speech lock after dispatching
            await _speechLock.UnlockAsync(cancellationToken);
        }
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

        // STEP 1: Fast transcription for wake word detection
        try
        {
            _logger.LogInformation("Fast transcription (wake word detection)...");
            var fastResult = await _transcription.TranscribeFastAsync(audioData);

            if (!fastResult.Success || string.IsNullOrWhiteSpace(fastResult.Text))
            {
                _logger.LogDebug("No speech detected (fast model)");
                ResetToWaiting();
                return;
            }

            _logger.LogInformation("Fast transcript: \"{Text}\"", fastResult.Text);
            
            // Detect wake word from fast transcription
            var wakeResult = _wakeWord.Detect(fastResult.Text);
            
            if (!wakeResult.Detected)
            {
                ResetToWaiting();
                return;
            }

            _logger.LogInformation("🎯 WAKE WORD: \"{WakeWord}\" (fast model)", wakeResult.WakeWord);
            
            // Stop any playing TTS immediately
            await _ttsControl.StopAsync(cancellationToken);
            
            var wakeWordLower = wakeResult.WakeWord!.ToLowerInvariant();
            var isAudioResponseWakeWord = _options.AudioResponseWakeWords
                .Any(w => w.Equals(wakeWordLower, StringComparison.OrdinalIgnoreCase));
            var isOpenCodeWakeWord = _options.OpenCodeWakeWords
                .Any(w => w.Equals(wakeWordLower, StringComparison.OrdinalIgnoreCase));

            if (isAudioResponseWakeWord)
            {
                // "Počítači" - play audio acknowledgment IMMEDIATELY
                _logger.LogInformation("🔊 Playing audio acknowledgment for '{WakeWord}'", wakeResult.WakeWord);
                await _wakeWordResponse.PlayResponseAsync(wakeWordLower, cancellationToken);
                
                // TODO: Future - wait for follow-up command after acknowledgment
                if (!string.IsNullOrWhiteSpace(wakeResult.Command))
                {
                    _logger.LogInformation("📝 Command after '{WakeWord}': \"{Command}\" (not dispatched yet)", 
                        wakeResult.WakeWord, wakeResult.Command);
                }
                ResetToWaiting();
            }
            else if (isOpenCodeWakeWord)
            {
                // "OpenCode" - lock speech and play acknowledgment IMMEDIATELY
                _logger.LogInformation("🔊 Playing OpenCode acknowledgment");
                
                // Lock speech to prevent TTS while collecting command
                await _speechLock.LockAsync($"WakeWord:{wakeResult.WakeWord}", cancellationToken);
                
                // Play ACK immediately (before accurate transcription)
                await _wakeWordResponse.PlayResponseAsync(wakeWordLower, cancellationToken);
                
                // STEP 2: Now do accurate transcription for the command
                // Only if there might be a command after wake word
                string? accurateCommand = null;
                if (!string.IsNullOrWhiteSpace(wakeResult.Command))
                {
                    _logger.LogInformation("Accurate transcription for command...");
                    var accurateResult = await _transcription.TranscribeAsync(audioData);
                    
                    if (accurateResult.Success && !string.IsNullOrWhiteSpace(accurateResult.Text))
                    {
                        _logger.LogInformation("Accurate transcript: \"{Text}\"", accurateResult.Text);
                        
                        // Re-detect wake word to get accurate command
                        var accurateWakeResult = _wakeWord.Detect(accurateResult.Text);
                        if (accurateWakeResult.Detected && !string.IsNullOrWhiteSpace(accurateWakeResult.Command))
                        {
                            accurateCommand = accurateWakeResult.Command;
                        }
                    }
                }
                
                // Add command if found
                if (!string.IsNullOrWhiteSpace(accurateCommand))
                {
                    _openCodeCommandParts.Clear();
                    _openCodeCommandParts.Add(accurateCommand);
                    _logger.LogInformation("📝 Initial command (accurate): \"{Command}\"", accurateCommand);
                }
                else if (!string.IsNullOrWhiteSpace(wakeResult.Command))
                {
                    // Fallback to fast transcription command
                    _openCodeCommandParts.Clear();
                    _openCodeCommandParts.Add(wakeResult.Command);
                    _logger.LogInformation("📝 Initial command (fast): \"{Command}\"", wakeResult.Command);
                }
                
                // Transition to collecting more command segments
                TransitionToCollectingOpenCodeCommand();
            }
            else
            {
                _logger.LogWarning("Wake word '{WakeWord}' detected but not configured in any handler", wakeResult.WakeWord);
                ResetToWaiting();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transcription failed");
            ResetToWaiting();
        }
    }

    private void ResetToWaiting()
    {
        _state = State.Waiting;
        _speechBuffer.Clear();
        _speechBufferBytes = 0;
        _silenceStart = default;
        _openCodeCommandParts.Clear();

        _logger.LogInformation("→ WAITING");
    }
}
