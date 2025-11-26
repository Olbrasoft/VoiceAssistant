using Microsoft.AspNetCore.SignalR.Client;
using Olbrasoft.VoiceAssistant.Orchestration.Models;
using Olbrasoft.VoiceAssistant.Orchestration.Services;

namespace Olbrasoft.VoiceAssistant.Orchestration;

/// <summary>
/// Voice assistant orchestrator.
/// Connects to WakeWordDetection service and orchestrates voice interactions.
/// </summary>
public class VoiceAssistantOrchestrator : IOrchestrator
{
    private readonly ILogger<VoiceAssistantOrchestrator> _logger;
    private readonly AudioResponsePlayer _audioPlayer;
    private readonly IConfiguration _configuration;
    private HubConnection? _hubConnection;
    
    public VoiceAssistantOrchestrator(
        ILogger<VoiceAssistantOrchestrator> logger,
        AudioResponsePlayer audioPlayer,
        IConfiguration configuration)
    {
        _logger = logger;
        _audioPlayer = audioPlayer;
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
    /// Handles wake word detection events.
    /// Plays appropriate audio response based on detected wake word.
    /// </summary>
    private async void OnWakeWordDetected(WakeWordEvent wakeWordEvent)
    {
        _logger.LogInformation(
            "Wake word detected: {Word} (Confidence: {Confidence:F2}, Time: {Time})",
            wakeWordEvent.Word,
            wakeWordEvent.Confidence,
            wakeWordEvent.DetectedAt);
        
        try
        {
            // Determine audio file based on wake word
            var audioFile = GetAudioFileForWakeWord(wakeWordEvent.Word);
            
            if (!string.IsNullOrEmpty(audioFile))
            {
                await _audioPlayer.PlayAsync(audioFile);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling wake word event");
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
            _logger.LogInformation("Jarvis detected - using male voice response");
            return "ano.mp3";  // Male voice: "Ano" (Yes)
        }
        
        // Check if wake word contains "alexa" -> female voice
        if (wakeWord.Contains("alexa", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Alexa detected - using female voice response");
            return "yes.mp3";  // Female voice: "Yes"
        }
        
        _logger.LogWarning("Unknown wake word: {WakeWord}, no audio response configured", wakeWord);
        return string.Empty;
    }
}
