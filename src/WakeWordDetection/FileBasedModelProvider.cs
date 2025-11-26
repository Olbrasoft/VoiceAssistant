using System.Globalization;
using System.Text.RegularExpressions;

namespace Olbrasoft.VoiceAssistant.WakeWordDetection;

/// <summary>
/// File-based implementation of IWakeWordModelProvider.
/// Parses model metadata from filename format: modelname_v{version}_t{threshold}.onnx
/// </summary>
public class FileBasedModelProvider : IWakeWordModelProvider
{
    private static readonly Regex ThresholdRegex = new(@"_t(0?\.\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex VersionRegex = new(@"_v(\d+\.\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    private readonly float _defaultThreshold;
    
    public FileBasedModelProvider(float defaultThreshold = 0.5f)
    {
        _defaultThreshold = defaultThreshold;
    }
    
    public WakeWordModel GetModel(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("Identifier cannot be null or empty.", nameof(identifier));
        }
        
        var filename = Path.GetFileName(identifier);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(identifier);
        
        var model = new WakeWordModel
        {
            Name = nameWithoutExtension,
            FilePath = identifier,
            Version = ParseVersion(filename),
            Threshold = _defaultThreshold,
            HasExplicitThreshold = false
        };
        
        // Try to parse threshold from filename
        var parsedThreshold = ParseThreshold(filename);
        if (parsedThreshold.HasValue)
        {
            model.Threshold = parsedThreshold.Value;
            model.HasExplicitThreshold = true;
        }
        
        return model;
    }
    
    public IEnumerable<WakeWordModel> GetModels(IEnumerable<string> identifiers)
    {
        return identifiers.Select(GetModel);
    }
    
    /// <summary>
    /// Parses threshold from filename using pattern _t{value}.
    /// Example: alexa_v0.1_t0.6.onnx -> 0.6
    /// </summary>
    private static float? ParseThreshold(string filename)
    {
        var match = ThresholdRegex.Match(filename);
        
        if (match.Success && float.TryParse(
            match.Groups[1].Value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var threshold))
        {
            return threshold;
        }
        
        return null;
    }
    
    /// <summary>
    /// Parses version from filename using pattern _v{version}.
    /// Example: alexa_v0.1_t0.6.onnx -> "0.1"
    /// </summary>
    private static string? ParseVersion(string filename)
    {
        var match = VersionRegex.Match(filename);
        return match.Success ? match.Groups[1].Value : null;
    }
}
