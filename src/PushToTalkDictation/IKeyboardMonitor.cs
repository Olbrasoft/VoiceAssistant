namespace Olbrasoft.VoiceAssistant.PushToTalkDictation;

/// <summary>
/// Interface for monitoring keyboard events (cross-platform abstraction).
/// </summary>
public interface IKeyboardMonitor : IDisposable
{
    /// <summary>
    /// Event raised when a key is pressed.
    /// </summary>
    event EventHandler<KeyEventArgs>? KeyPressed;

    /// <summary>
    /// Event raised when a key is released.
    /// </summary>
    event EventHandler<KeyEventArgs>? KeyReleased;

    /// <summary>
    /// Gets a value indicating whether keyboard monitoring is currently active.
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// Starts monitoring keyboard events.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StartMonitoringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops monitoring keyboard events.
    /// </summary>
    Task StopMonitoringAsync();

    /// <summary>
    /// Gets the current state of CapsLock.
    /// </summary>
    /// <returns>True if CapsLock is ON (LED lit), false if OFF.</returns>
    bool IsCapsLockOn();
}
