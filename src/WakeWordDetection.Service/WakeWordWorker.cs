using Olbrasoft.VoiceAssistant.WakeWordDetection;
using Olbrasoft.VoiceAssistant.WakeWordDetection.Service.Models;
using Olbrasoft.VoiceAssistant.WakeWordDetection.Service.Services;

namespace Olbrasoft.VoiceAssistant.WakeWordDetection.Service;

/// <summary>
/// Background service that continuously listens for wake word detections
/// and broadcasts events to connected clients via SignalR.
/// </summary>
public class WakeWordWorker : BackgroundService
{
    private readonly IWakeWordDetector _detector;
    private readonly IEventBroadcastService _eventBroadcast;
    private readonly ILogger<WakeWordWorker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WakeWordWorker"/> class.
    /// </summary>
    /// <param name="detector">Wake word detector instance.</param>
    /// <param name="eventBroadcast">Service for broadcasting events to clients.</param>
    /// <param name="logger">Logger for worker operations.</param>
    public WakeWordWorker(
        IWakeWordDetector detector,
        IEventBroadcastService eventBroadcast,
        ILogger<WakeWordWorker> logger)
    {
        _detector = detector;
        _eventBroadcast = eventBroadcast;
        _logger = logger;
    }

    /// <summary>
    /// Executes the background service, starting the wake word detection
    /// and subscribing to detection events.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token to stop the service.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _detector.WakeWordDetected += OnWakeWordDetected;
        
        _logger.LogInformation("🎤 WakeWord Listener starting - API will be available on http://localhost:5000");
        
        await _detector.StartListeningAsync(stoppingToken);
    }

    /// <summary>
    /// Event handler called when a wake word is detected.
    /// Creates a wake word event and broadcasts it to all connected clients.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Wake word detection event arguments.</param>
    private async void OnWakeWordDetected(object? sender, WakeWordDetectedEventArgs e)
    {
        // Rozeslat událost přes WebSocket všem klientům
        var wakeWordEvent = new WakeWordEvent
        {
            Word = e.DetectedWord,
            DetectedAt = e.DetectedAt,
            Confidence = e.Confidence,
            ServiceVersion = "1.0.0"
        };
        
        await _eventBroadcast.BroadcastWakeWordDetectedAsync(wakeWordEvent);
    }

    /// <summary>
    /// Stops the background service and cleans up resources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping WakeWord Listener...");
        await _detector.StopListeningAsync();
        await base.StopAsync(cancellationToken);
    }
}
