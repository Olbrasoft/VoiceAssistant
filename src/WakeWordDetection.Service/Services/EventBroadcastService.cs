using Microsoft.AspNetCore.SignalR;
using Olbrasoft.VoiceAssistant.WakeWordDetection.Service.Hubs;
using Olbrasoft.VoiceAssistant.WakeWordDetection.Service.Models;

namespace Olbrasoft.VoiceAssistant.WakeWordDetection.Service.Services;

/// <summary>
/// Service interface for broadcasting wake word detection events to connected clients.
/// </summary>
public interface IEventBroadcastService
{
    /// <summary>
    /// Broadcasts a wake word detection event to all connected SignalR clients.
    /// </summary>
    /// <param name="wakeWordEvent">The wake word event to broadcast.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task BroadcastWakeWordDetectedAsync(WakeWordEvent wakeWordEvent);
}

/// <summary>
/// Implementation of event broadcast service using SignalR.
/// Sends wake word detection events to all connected clients via WebSocket.
/// </summary>
public class EventBroadcastService : IEventBroadcastService
{
    private readonly IHubContext<WakeWordHub> _hubContext;
    private readonly ILogger<EventBroadcastService> _logger;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="EventBroadcastService"/> class.
    /// </summary>
    /// <param name="hubContext">SignalR hub context for sending messages to clients.</param>
    /// <param name="logger">Logger for broadcast operations.</param>
    public EventBroadcastService(
        IHubContext<WakeWordHub> hubContext,
        ILogger<EventBroadcastService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }
    
    /// <inheritdoc />
    public async Task BroadcastWakeWordDetectedAsync(WakeWordEvent wakeWordEvent)
    {
        _logger.LogInformation("📡 Broadcasting wake word event: {Word}", wakeWordEvent.Word);
        
        // Odeslat všem připojeným klientům
        await _hubContext.Clients.All.SendAsync("WakeWordDetected", wakeWordEvent);
        
        _logger.LogInformation("✅ Broadcast completed for: {Word}", wakeWordEvent.Word);
    }
}
