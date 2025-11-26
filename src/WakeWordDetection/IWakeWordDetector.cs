namespace Olbrasoft.VoiceAssistant.WakeWordDetection;

/// <summary>
/// Interface for wake word detection implementations.
/// </summary>
public interface IWakeWordDetector : IDisposable
{
    /// <summary>
    /// Event raised when a wake word is detected.
    /// </summary>
    event EventHandler<WakeWordDetectedEventArgs>? WakeWordDetected;
    
    /// <summary>
    /// Starts listening for wake words asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop listening.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartListeningAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stops listening for wake words asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopListeningAsync();
    
    /// <summary>
    /// Gets a value indicating whether the detector is currently listening.
    /// </summary>
    bool IsListening { get; }
    
    /// <summary>
    /// Gets the collection of configured wake words.
    /// </summary>
    /// <returns>Read-only collection of wake words.</returns>
    IReadOnlyCollection<string> GetWakeWords();
}
