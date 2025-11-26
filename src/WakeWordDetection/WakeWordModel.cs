namespace Olbrasoft.VoiceAssistant.WakeWordDetection;

/// <summary>
/// Data Transfer Object representing a wake word model with its configuration.
/// </summary>
public class WakeWordModel
{
    /// <summary>
    /// Name of the model (e.g., "alexa_v0.1_t0.6").
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Full file path to the ONNX model.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Model version (e.g., "0.1").
    /// </summary>
    public string? Version { get; set; }
    
    /// <summary>
    /// Detection threshold for this model (0.0 - 1.0).
    /// Parsed from filename (e.g., _t0.6) or set to default.
    /// </summary>
    public float Threshold { get; set; }
    
    /// <summary>
    /// Whether the threshold was explicitly specified in filename.
    /// </summary>
    public bool HasExplicitThreshold { get; set; }
}
