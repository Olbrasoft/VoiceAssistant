using Olbrasoft.VoiceAssistant.ContinuousListener.Services;
using VoiceAssistant.Shared.Data.Enums;

namespace Olbrasoft.VoiceAssistant.ContinuousListener;

/// <summary>
/// Main worker that continuously listens for speech, transcribes it,
/// and uses LLM Router to determine actions (OpenCode, Respond, Bash, Ignore).
/// </summary>
public class ContinuousListenerWorker : BackgroundService
{
    private readonly ILogger<ContinuousListenerWorker> _logger;
    private readonly AudioCaptureService _audioCapture;
    private readonly VadService _vad;
    private readonly TranscriptionService _transcription;
    private readonly ILlmRouterService _llmRouter;
    private readonly CommandDispatcher _dispatcher;
    private readonly TtsPlaybackService _ttsPlayback;
    private readonly TtsControlService _ttsControl;
    private readonly SpeechLockService _speechLock;
    private readonly AssistantSpeechStateService _assistantSpeechState;
    private readonly AssistantSpeechTrackerService _speechTracker;
    // BashExecutionService removed - all commands now go through OpenCode
    private readonly ContinuousListenerOptions _options;

    // State machine - simplified without wake words
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

    // Stop commands - words that immediately stop TTS playback
    private static readonly HashSet<string> StopCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "stop", "stůj", "ticho", "dost", "přestaň", "zastav"
    };

    public ContinuousListenerWorker(
        ILogger<ContinuousListenerWorker> logger,
        IConfiguration configuration,
        AudioCaptureService audioCapture,
        VadService vad,
        TranscriptionService transcription,
        ILlmRouterService llmRouter,
        CommandDispatcher dispatcher,
        TtsPlaybackService ttsPlayback,
        TtsControlService ttsControl,
        SpeechLockService speechLock,
        AssistantSpeechStateService assistantSpeechState,
        AssistantSpeechTrackerService speechTracker)
    {
        _logger = logger;
        _audioCapture = audioCapture;
        _vad = vad;
        _transcription = transcription;
        _llmRouter = llmRouter;
        _dispatcher = dispatcher;
        _ttsPlayback = ttsPlayback;
        _ttsControl = ttsControl;
        _speechLock = speechLock;
        _assistantSpeechState = assistantSpeechState;
        _speechTracker = speechTracker;

        _options = new ContinuousListenerOptions();
        configuration.GetSection(ContinuousListenerOptions.SectionName).Bind(_options);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
                    await TransitionToRecordingAsync(rms, cancellationToken);
                    
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
                            await ResetToWaitingAsync(cancellationToken);
                        }
                    }
                }
                break;
        }
    }

    private async Task TransitionToRecordingAsync(float probability, CancellationToken cancellationToken)
    {
        // Note: We no longer block recording when TTS history is not empty.
        // Instead, we record everything and filter out TTS echo from transcription.
        // This allows capturing user speech that overlaps with or follows TTS.
        
        _state = State.Recording;
        _recordingStart = DateTime.UtcNow;
        _silenceStart = default;
        
        // Lock TTS immediately when speech is detected
        await _speechLock.LockAsync("Recording", cancellationToken);
    }

    private async Task CompleteRecordingAsync(CancellationToken cancellationToken)
    {
        _segmentCount++;
        var duration = DateTime.UtcNow - _recordingStart;

        // Combine all chunks
        var audioData = new byte[_speechBufferBytes];
        int offset = 0;
        foreach (var chunk in _speechBuffer)
        {
            Buffer.BlockCopy(chunk, 0, audioData, offset, chunk.Length);
            offset += chunk.Length;
        }

        try
        {
            // Transcribe speech
            var transcriptionResult = await _transcription.TranscribeAsync(audioData);

            if (!transcriptionResult.Success || string.IsNullOrWhiteSpace(transcriptionResult.Text))
            {
                _speechTracker.ClearHistory();
                await ResetToWaitingAsync(cancellationToken);
                return;
            }

            var text = transcriptionResult.Text;
            
            // Bright cyan for original transcript from Whisper
            Console.WriteLine($"\u001b[96;1m📝 \"{text}\"\u001b[0m");

            // Filter out TTS echo(es) from the transcription
            var filteredText = _speechTracker.FilterEchoFromTranscription(text);
            
            if (string.IsNullOrWhiteSpace(filteredText))
            {
                // Pure echo, no user speech
                Console.WriteLine($"\u001b[90m🔇 Filtered out - only echo detected\u001b[0m");
                _speechTracker.ClearHistory();
                await ResetToWaitingAsync(cancellationToken);
                return;
            }
            
            // Update text with filtered version
            if (filteredText != text)
            {
                Console.WriteLine($"\u001b[93m🔇 After echo removal: \"{filteredText}\"\u001b[0m");
                text = filteredText;
            }

            // Local pre-filter: skip short/noise phrases before calling LLM API
            if (ShouldSkipLocally(text))
            {
                Console.WriteLine($"\u001b[90m⏭️ Skipped locally (too short or noise)\u001b[0m");
                _speechTracker.ClearHistory();
                await ResetToWaitingAsync(cancellationToken);
                return;
            }

            // Check for STOP command - this is handled locally, not sent to LLM
            if (IsStopCommand(text))
            {
                // Check if the stop word came from TTS (echo) or from user
                if (_speechTracker.ContainsStopWord(StopCommands))
                {
                    // TTS said "stop" - this is echo, ignore it
                    Console.WriteLine($"\u001b[90m🔇 Stop command from TTS echo - ignoring\u001b[0m");
                    _speechTracker.ClearHistory();
                    await ResetToWaitingAsync(cancellationToken);
                    return;
                }
                else
                {
                    // User said "stop" - stop TTS playback immediately
                    Console.WriteLine($"\u001b[91;1m🛑 User STOP command - stopping TTS playback\u001b[0m");
                    await _ttsControl.StopAsync(cancellationToken);
                    _speechTracker.ClearHistory();
                    await ResetToWaitingAsync(cancellationToken);
                    return;
                }
            }

            // Clear TTS history before sending to LLM (we're done with echo filtering)
            _speechTracker.ClearHistory();

            // Bílá barva pro text odesílaný na LLM
            Console.WriteLine($"\u001b[97;1m🤖 → LLM: \"{text}\"\u001b[0m");

            // Route through LLM (Cerebras or Groq)
            var routerResult = await _llmRouter.RouteAsync(text, cancellationToken);

            // Barevný výpis rozhodnutí LLM routeru
            var actionColor = routerResult.Action == LlmRouterAction.Ignore 
                ? "\u001b[91;1m"  // Červená pro IGNORE
                : "\u001b[92;1m"; // Zelená pro akce (OpenCode, Respond, Bash)
            Console.WriteLine($"{actionColor}🎯 {_llmRouter.ProviderName}: {routerResult.Action.ToString().ToUpper()} (confidence: {routerResult.Confidence:F2}, {routerResult.ResponseTimeMs}ms)\u001b[0m");
            
            if (!string.IsNullOrEmpty(routerResult.Reason))
            {
                Console.WriteLine($"{actionColor}   └─ {routerResult.Reason}\u001b[0m");
            }

            // Stop any currently playing TTS before processing
            await _ttsControl.StopAsync(cancellationToken);

            switch (routerResult.Action)
            {
                case LlmRouterAction.OpenCode:
                    await HandleOpenCodeActionAsync(text, routerResult.IsQuestion, cancellationToken);
                    break;

                case LlmRouterAction.Respond:
                    await HandleRespondActionAsync(routerResult.Response, cancellationToken);
                    break;

                case LlmRouterAction.Bash:
                    // Bash actions are now redirected to OpenCode
                    // OpenCode has full context and can execute commands properly
                    Console.WriteLine($"\u001b[93;1m⚠️ Bash action redirected to OpenCode\u001b[0m");
                    await HandleOpenCodeActionAsync(text, routerResult.IsQuestion, cancellationToken);
                    break;

                case LlmRouterAction.Ignore:
                    // Už je vypsáno červeně výše
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing speech");
        }
        finally
        {
            await ResetToWaitingAsync(cancellationToken);
        }
    }

    private async Task HandleOpenCodeActionAsync(string command, bool isQuestion, CancellationToken cancellationToken)
    {
        if (isQuestion)
        {
            // Questions: send with PLAN MODE prefix and don't auto-submit
            // This allows user to review OpenCode's plan before execution
            Console.WriteLine($"\u001b[96;1m❓ Question detected - sending in PLAN MODE\u001b[0m");
            var planModeCommand = $"PLAN MODE: {command}";
            await _dispatcher.DispatchAsync(planModeCommand, submitPrompt: false, cancellationToken);
        }
        else
        {
            // Commands: send directly and auto-submit for immediate execution
            await _dispatcher.DispatchAsync(command, submitPrompt: true, cancellationToken);
        }
    }

    private async Task HandleRespondActionAsync(string? response, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            _logger.LogWarning("LLM returned RESPOND but no response text");
            return;
        }

        // Bright green for responses
        Console.WriteLine($"\u001b[92;1m🔊 \"{response}\"\u001b[0m");

        // Release speech lock before TTS so we don't block ourselves
        await _speechLock.UnlockAsync(cancellationToken);

        // Speak the response
        await _ttsPlayback.SpeakAsync(response, cancellationToken);
    }

    // HandleBashActionAsync removed - all bash commands now go through OpenCode
    // OpenCode has full context (current directory, project state, etc.) 
    // and can execute commands properly

    private async Task ResetToWaitingAsync(CancellationToken cancellationToken)
    {
        _state = State.Waiting;
        _speechBuffer.Clear();
        _speechBufferBytes = 0;
        _silenceStart = default;

        // Release speech lock when going back to waiting
        await _speechLock.UnlockAsync(cancellationToken);

        // Gray color for less important info
        Console.WriteLine("\u001b[90m→ WAITING\u001b[0m");
    }

    /// <summary>
    /// Local pre-filter to skip noise phrases before calling LLM API.
    /// This saves API calls for obvious non-commands.
    /// Note: Short texts are allowed (user requested to keep them).
    /// </summary>
    private bool ShouldSkipLocally(string text)
    {
        var normalized = text.Trim().ToLowerInvariant();
        
        // Blacklist of common noise phrases (exact matches after normalization)
        var noisePatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ano", "ne", "jo", "no", "tak", "hmm", "hm", "aha", "ok", "okay",
            "dobře", "jasně", "fajn", "super", "díky", "děkuji", "prosím",
            "moment", "počkej", "ehm", "ehm ehm", "no tak", "tak jo",
            "to je", "to bylo", "a tak", "no jo", "no ne", "tak tak",
            "jasně jasně", "jo jo", "ne ne", "aha aha", "mm", "mhm",
            "no nic", "nic", "nevím", "uvidíme", "možná", "asi",
            "co to", "co je", "hele", "hele hele", "víš co", "že jo",
            "no jasně", "no dobře", "no fajn", "to jo", "to ne",
            "tak nějak", "nějak", "prostě", "vlastně", "takže",
            // Common transcription artifacts
            "...", ".", ",", "!", "?"
        };

        // Check for exact match with noise
        if (noisePatterns.Contains(normalized.TrimEnd('.', ',', '!', '?', ' ')))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the text contains a stop command (stop, stůj, ticho, dost, etc.).
    /// The stop command can be anywhere in the text (e.g., "Počítači, stop").
    /// </summary>
    internal static bool IsStopCommand(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
            
        var normalized = text.Trim().ToLowerInvariant();
        
        // Split into words and check if any word is a stop command
        var words = normalized.Split(new[] { ' ', ',', '.', '!', '?', ';', ':' }, 
            StringSplitOptions.RemoveEmptyEntries);
            
        foreach (var word in words)
        {
            if (StopCommands.Contains(word))
                return true;
        }
        
        return false;
    }
}
