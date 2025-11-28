using Microsoft.AspNetCore.SignalR;

namespace Olbrasoft.VoiceAssistant.PushToTalkDictation.Service.Hubs;

/// <summary>
/// SignalR hub for real-time Push-to-Talk dictation events.
/// Provides WebSocket endpoint for clients to receive PTT notifications.
/// </summary>
public class PttHub : Hub
{
    private readonly ILogger<PttHub> _logger;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PttHub"/> class.
    /// </summary>
    /// <param name="logger">Logger for connection events.</param>
    public PttHub(ILogger<PttHub> logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Called when a client connects to the hub.
    /// Sends a connection confirmation message to the client.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }
    
    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    /// <param name="exception">Exception that caused the disconnection, if any.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
    
    /// <summary>
    /// Allows a client to subscribe to PTT events with a custom name.
    /// </summary>
    /// <param name="clientName">Name of the subscribing client.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Subscribe(string clientName)
    {
        _logger.LogInformation("Client {ClientName} subscribed", clientName);
        await Clients.Caller.SendAsync("Subscribed", clientName);
    }
}
