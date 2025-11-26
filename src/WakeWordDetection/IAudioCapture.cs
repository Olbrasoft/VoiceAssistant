namespace Olbrasoft.VoiceAssistant.WakeWordDetection;

/// <summary>
/// Interface for audio capture from microphone.
/// </summary>
public interface IAudioCapture : IDisposable
{
    /// <summary>
    /// Event raised when audio data is available.
    /// </summary>
    event EventHandler<AudioDataEventArgs>? AudioDataAvailable;

    /// <summary>
    /// Gets the sample rate in Hz.
    /// </summary>
    int SampleRate { get; }

    /// <summary>
    /// Gets the number of audio channels (1 for mono, 2 for stereo).
    /// </summary>
    int Channels { get; }

    /// <summary>
    /// Gets the bits per sample (typically 16).
    /// </summary>
    int BitsPerSample { get; }

    /// <summary>
    /// Gets a value indicating whether audio capture is currently active.
    /// </summary>
    bool IsCapturing { get; }

    /// <summary>
    /// Starts capturing audio from the microphone.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StartCaptureAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops capturing audio from the microphone.
    /// </summary>
    Task StopCaptureAsync();
}
