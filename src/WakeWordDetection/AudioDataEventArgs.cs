namespace Olbrasoft.VoiceAssistant.WakeWordDetection;

/// <summary>
/// Event arguments for audio data availability.
/// </summary>
public class AudioDataEventArgs : EventArgs
{
    /// <summary>
    /// Gets the audio data as 16-bit PCM samples.
    /// </summary>
    public required short[] AudioData { get; init; }

    /// <summary>
    /// Gets the number of samples in the audio data.
    /// </summary>
    public int SampleCount => AudioData.Length;

    /// <summary>
    /// Gets the timestamp when the audio data was captured.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
