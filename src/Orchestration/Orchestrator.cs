using Microsoft.AspNetCore.SignalR.Client;
using Olbrasoft.VoiceAssistant.Orchestration.Models;
using Olbrasoft.VoiceAssistant.Orchestration.Services;

namespace Olbrasoft.VoiceAssistant.Orchestration;

/// <summary>
/// Voice assistant orchestrator.
/// Connects to WakeWordDetection service and orchestrates voice interactions.
/// </summary>
public class Orchestrator : IOrchestrator
{
    private readonly ILogger<Orchestrator> _logger;
    private readonly SpeechRecognitionService _speechRecognition;
    private readonly TextInputService _textInput;
    private readonly IConfiguration _configuration;
    private HubConnection? _hubConnection;
    private readonly SemaphoreSlim _workflowLock = new(1, 1); // Prevent concurrent wake word handling
    private DateTime _lastWakeWordTime = DateTime.MinValue; // Track last wake word time for debouncing
    private readonly TimeSpan _debounceInterval = TimeSpan.FromSeconds(3); // Ignore wake words within 3 seconds
    private int _isProcessing = 0; // Atomic flag to prevent multiple async void calls

    public Orchestrator(
        ILogger<Orchestrator> logger,
        SpeechRecognitionService speechRecognition,
        TextInputService textInput,
        IConfiguration configuration)
    {
        _logger = logger;
        _speechRecognition = speechRecognition;
        _textInput = textInput;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var wakeWordServiceUrl = _configuration["WakeWordServiceUrl"] ?? "http://localhost:5000";
        var hubUrl = $"{wakeWordServiceUrl}/hubs/wakeword";

        _logger.LogInformation("Connecting to WakeWordDetection service at {Url}", hubUrl);

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        // Handle WakeWordDetected event
        _hubConnection.On<WakeWordEvent>("WakeWordDetected", OnWakeWordDetected);

        // Handle connection events
        _hubConnection.Closed += async (error) =>
        {
            _logger.LogWarning("Connection closed: {Error}", error?.Message);
            await Task.Delay(5000, cancellationToken);
        };

        _hubConnection.Reconnecting += error =>
        {
            _logger.LogWarning("Reconnecting: {Error}", error?.Message);
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += connectionId =>
        {
            _logger.LogInformation("Reconnected: {ConnectionId}", connectionId);
            return Task.CompletedTask;
        };

        try
        {
            await _hubConnection.StartAsync(cancellationToken);
            _logger.LogInformation("Connected to WakeWordDetection service");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to WakeWordDetection service");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_hubConnection != null)
        {
            _logger.LogInformation("Disconnecting from WakeWordDetection service");
            await _hubConnection.StopAsync(cancellationToken);
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    /// <summary>
    /// Manually triggers voice dictation workflow (same as wake word detection).
    /// Can be called by API endpoints or other external triggers.
    /// </summary>
    public async Task TriggerDictationAsync()
    {
        _logger.LogInformation("🎤 Manual dictation triggered via API");
        
        // ATOMIC CHECK: Prevent multiple async calls from even starting
        if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) != 0)
        {
            _logger.LogWarning("Dictation already in progress, ignoring trigger");
            return;
        }

        try
        {
            // FIRST: Prevent concurrent workflow handling (get lock immediately)
            if (!await _workflowLock.WaitAsync(0))
            {
                _logger.LogWarning("Workflow lock already acquired, ignoring trigger");
                return;
            }

            try
            {
                var now = DateTime.UtcNow;

                // SECOND: Debouncing inside the lock (prevents race condition)
                if (now - _lastWakeWordTime < _debounceInterval)
                {
                    _logger.LogInformation("Debounce interval not elapsed, ignoring trigger");
                    return;
                }

                _lastWakeWordTime = now;

                // 1. Play "Ano, poslouchám" audio confirmation
                try
                {
                    var audioFile = "/home/jirka/Olbrasoft/VoiceAssistant/assets/audio/ano-posloucham.mp3";
                    var processStartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "ffplay",
                        Arguments = $"-nodisp -autoexit -loglevel quiet \"{audioFile}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    
                    using var process = System.Diagnostics.Process.Start(processStartInfo);
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                    }
                    
                    // Wait for audio echo to clear
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to play audio confirmation");
                }

                // 2. Record audio and transcribe to text
                var transcribedText = await _speechRecognition.RecordAndTranscribeAsync();

                if (string.IsNullOrWhiteSpace(transcribedText))
                {
                    _logger.LogInformation("No speech detected or transcription failed");
                    return;
                }

                // 3. Type the transcribed text into focused window (or send to OpenCode)
                var autoSubmit = _configuration.GetValue<bool>("OpenCodeAutoSubmit", true);
                var success = await _textInput.TypeTextAsync(transcribedText, autoSubmit);

                if (success)
                {
                    _logger.LogInformation("✅ Manual dictation workflow completed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in manual dictation workflow");
                throw; // Re-throw for API error handling
            }
            finally
            {
                _workflowLock.Release();
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isProcessing, 0);
        }
    }

    /// <summary>
    /// Event handler for WakeWordDetected SignalR event.
    /// Plays audio response, records speech, transcribes, and types text.
    /// Uses atomic flag + semaphore lock + debouncing to prevent concurrent/duplicate execution.
    /// </summary>
    private async void OnWakeWordDetected(WakeWordEvent wakeWordEvent)
    {
        _logger.LogInformation("🔔 Wake word detected: {Word}", wakeWordEvent.Word);

        // ATOMIC CHECK: Prevent multiple async void calls from even starting
        if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) != 0)
        {
            return;
        }

        try
        {
            // FIRST: Prevent concurrent wake word handling (get lock immediately)
            if (!await _workflowLock.WaitAsync(0))
            {
                return;
            }

            try
            {
                var now = DateTime.UtcNow;

                // SECOND: Debouncing inside the lock (prevents race condition)
                if (now - _lastWakeWordTime < _debounceInterval)
                {
                    return;
                }

                _lastWakeWordTime = now;

                // 1. Play "Ano, poslouchám" audio confirmation
                try
                {
                    var audioFile = "/home/jirka/Olbrasoft/VoiceAssistant/assets/audio/ano-posloucham.mp3";
                    var processStartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "ffplay",
                        Arguments = $"-nodisp -autoexit -loglevel quiet \"{audioFile}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    
                    using var process = System.Diagnostics.Process.Start(processStartInfo);
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                    }
                    
                    // Wait for audio echo to clear
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to play audio confirmation");
                }

                // 2. Record audio and transcribe to text
                var transcribedText = await _speechRecognition.RecordAndTranscribeAsync();

                if (string.IsNullOrWhiteSpace(transcribedText))
                {
                    return;
                }

                // 3. Type the transcribed text into focused window (or send to OpenCode)
                var autoSubmit = _configuration.GetValue<bool>("OpenCodeAutoSubmit", true);
                var success = await _textInput.TypeTextAsync(transcribedText, autoSubmit);

                if (success)
                {
                    _logger.LogInformation("✅ Workflow completed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling wake word event");
            }
            finally
            {
                _workflowLock.Release();
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isProcessing, 0);
        }
    }

    /// <summary>
    /// Extracts friendly wake word name from detection string.
    /// </summary>
    /// <param name="wakeWord">Detected wake word (e.g., "hey_jarvis_v0.1_t0.5").</param>
    /// <returns>Friendly wake word name.</returns>
    private string GetWakeWordName(string wakeWord)
    {
        if (wakeWord.Contains("jarvis", StringComparison.OrdinalIgnoreCase))
        {
            return "Jarvis";
        }

        if (wakeWord.Contains("alexa", StringComparison.OrdinalIgnoreCase))
        {
            return "Alexa";
        }

        if (wakeWord.Contains("mycroft", StringComparison.OrdinalIgnoreCase))
        {
            return "Mycroft";
        }

        if (wakeWord.Contains("rhasspy", StringComparison.OrdinalIgnoreCase))
        {
            return "Rhasspy";
        }

        // Return the original if no match
        return wakeWord;
    }
}
