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
    private readonly AudioResponsePlayer _audioPlayer;
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
        AudioResponsePlayer audioPlayer,
        SpeechRecognitionService speechRecognition,
        TextInputService textInput,
        IConfiguration configuration)
    {
        _logger = logger;
        _audioPlayer = audioPlayer;
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
    /// Event handler for WakeWordDetected SignalR event.
    /// Plays audio response, records speech, transcribes, and types text.
    /// Uses atomic flag + semaphore lock + debouncing to prevent concurrent/duplicate execution.
    /// </summary>
    private async void OnWakeWordDetected(WakeWordEvent wakeWordEvent)
    {
        var eventId = Guid.NewGuid().ToString("N").Substring(0, 8);
        _logger.LogInformation("🔔 [Event {EventId}] Wake word event received: {Word}", eventId, wakeWordEvent.Word);

        // ATOMIC CHECK: Prevent multiple async void calls from even starting
        if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) != 0)
        {
            _logger.LogWarning("⚠️  [Event {EventId}] Wake word REJECTED - another event is being processed (atomic)", eventId);
            return;
        }

        try
        {
            // FIRST: Prevent concurrent wake word handling (get lock immediately)
            if (!await _workflowLock.WaitAsync(0))
            {
                _logger.LogWarning("⚠️  [Event {EventId}] Wake word ignored - workflow already in progress", eventId);
                return;
            }

            try
            {
                var now = DateTime.UtcNow;

                // SECOND: Debouncing inside the lock (prevents race condition)
                if (now - _lastWakeWordTime < _debounceInterval)
                {
                    _logger.LogWarning("⚠️  [Event {EventId}] Wake word ignored - debounced (too soon after previous: {TimeSinceLast:F1}s)",
                        eventId, (now - _lastWakeWordTime).TotalSeconds);
                    return;
                }

                _lastWakeWordTime = now; // Update timestamp INSIDE lock
                _logger.LogInformation("🎤 [Event {EventId}] Wake word ACCEPTED: {Word}", eventId, wakeWordEvent.Word);

                // 1. Play audio confirmation ("Ano" / "Yes")
                var audioFile = GetAudioFileForWakeWord(wakeWordEvent.Word);

                if (!string.IsNullOrEmpty(audioFile))
                {
                    _logger.LogInformation("🔊 [Event {EventId}] About to play audio: {File}", eventId, audioFile);
                    await _audioPlayer.PlayAsync(audioFile);
                    _logger.LogInformation("✅ [Event {EventId}] Audio playback completed", eventId);

                    // Wait for audio to finish and echo to clear (2500ms delay to prevent microphone capturing audio response)
                    await Task.Delay(2500);
                }

                // 2. Record audio and transcribe to text
                _logger.LogInformation("🎤 Starting speech recognition...");
                var transcribedText = await _speechRecognition.RecordAndTranscribeAsync();

                if (string.IsNullOrWhiteSpace(transcribedText))
                {
                    _logger.LogWarning("⚠️  No text transcribed, skipping typing");
                    return;
                }

                // 3. Type the transcribed text into focused window
                _logger.LogInformation("⌨️  Typing transcribed text...");
                var success = await _textInput.TypeTextAsync(transcribedText);

                if (success)
                {
                    _logger.LogInformation("✅ Speech-to-text workflow completed successfully");
                }
                else
                {
                    _logger.LogWarning("⚠️  Failed to type transcribed text");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [Event {EventId}] Error handling wake word event", eventId);
            }
            finally
            {
                // Release the semaphore lock
                _workflowLock.Release();
                _logger.LogInformation("🔓 [Event {EventId}] Workflow lock released", eventId);
            }
        }
        finally
        {
            // ALWAYS release the atomic flag
            Interlocked.Exchange(ref _isProcessing, 0);
            _logger.LogInformation("🔓 [Event {EventId}] Processing flag released", eventId);
        }
    }

    /// <summary>
    /// Maps wake word to appropriate audio response file.
    /// </summary>
    /// <param name="wakeWord">Detected wake word (e.g., "hey_jarvis_v0.1_t0.35").</param>
    /// <returns>Audio file name.</returns>
    private string GetAudioFileForWakeWord(string wakeWord)
    {
        // Check if wake word contains "jarvis" -> male voice
        if (wakeWord.Contains("jarvis", StringComparison.OrdinalIgnoreCase))
        {
            return "ano.mp3";  // Male voice: "Ano" (Yes)
        }

        // Check if wake word contains "alexa" -> female voice
        if (wakeWord.Contains("alexa", StringComparison.OrdinalIgnoreCase))
        {
            return "yes.mp3";  // Female voice: "Yes"
        }

        return string.Empty;
    }
}
