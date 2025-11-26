namespace Olbrasoft.VoiceAssistant.WakeWordDetection;

/// <summary>
/// Service that provides wake word models.
/// Implementation can load from files, database, remote API, etc.
/// </summary>
public interface IWakeWordModelProvider
{
    /// <summary>
    /// Gets a single wake word model by path/identifier.
    /// </summary>
    /// <param name="identifier">Model identifier (file path, database ID, etc.).</param>
    /// <returns>WakeWordModel with configuration.</returns>
    WakeWordModel GetModel(string identifier);
    
    /// <summary>
    /// Gets multiple wake word models.
    /// </summary>
    /// <param name="identifiers">Collection of model identifiers.</param>
    /// <returns>Collection of WakeWordModel objects.</returns>
    IEnumerable<WakeWordModel> GetModels(IEnumerable<string> identifiers);
}
