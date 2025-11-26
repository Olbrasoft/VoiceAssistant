namespace Olbrasoft.VoiceAssistant.Orchestration.Models;

/// <summary>
/// Wake word event received from WakeWordDetection service.
/// </summary>
public record WakeWordEvent
{
    /// <summary>
    /// Detected wake word (e.g., "hey_jarvis_v0.1_t0.35", "alexa_v0.1_t0.6").
    /// </summary>
    public string Word { get; init; } = string.Empty;
    
    /// <summary>
    /// UTC timestamp when the word was detected.
    /// </summary>
    public DateTime DetectedAt { get; init; }
    
    /// <summary>
    /// Detection confidence score (0.0 to 1.0).
    /// </summary>
    public float Confidence { get; init; }
    
    /// <summary>
    /// Version of the WakeWordDetection service.
    /// </summary>
    public string ServiceVersion { get; init; } = "1.0.0";
}
