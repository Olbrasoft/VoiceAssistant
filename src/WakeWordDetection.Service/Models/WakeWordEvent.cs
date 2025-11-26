namespace Olbrasoft.VoiceAssistant.WakeWordDetection.Service.Models;

/// <summary>
/// Data transfer object for wake word detection events.
/// </summary>
/// <param name="Word">The detected wake word.</param>
/// <param name="DetectedAt">The UTC timestamp when the word was detected.</param>
/// <param name="Confidence">The confidence score of the detection (0.0 to 1.0).</param>
/// <param name="ServiceVersion">The version of the service that detected the word.</param>
public record WakeWordEvent
{
    /// <summary>
    /// Gets the detected wake word.
    /// </summary>
    public string Word { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets the UTC timestamp when the word was detected.
    /// </summary>
    public DateTime DetectedAt { get; init; }
    
    /// <summary>
    /// Gets the confidence score of the detection (0.0 to 1.0).
    /// </summary>
    public float Confidence { get; init; }
    
    /// <summary>
    /// Gets the version of the service that detected the word.
    /// </summary>
    public string ServiceVersion { get; init; } = "1.0.0";
}
