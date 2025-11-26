namespace Olbrasoft.VoiceAssistant.WakeWordDetection;

/// <summary>
/// Event arguments containing information about detected wake word.
/// </summary>
public class WakeWordDetectedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the wake word that was detected.
    /// </summary>
    public string DetectedWord { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets the UTC timestamp when the wake word was detected.
    /// </summary>
    public DateTime DetectedAt { get; init; }
    
    /// <summary>
    /// Gets the confidence score of the detection (0.0 to 1.0).
    /// </summary>
    public float Confidence { get; init; }
}
