namespace Olbrasoft.VoiceAssistant.Orchestration;

/// <summary>
/// Interface for voice assistant orchestrator.
/// Coordinates wake word detection, audio responses, and voice processing.
/// </summary>
public interface IOrchestrator
{
    /// <summary>
    /// Starts the orchestrator and connects to WakeWordDetection service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task StartAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// Stops the orchestrator and disconnects from services.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken);
}
