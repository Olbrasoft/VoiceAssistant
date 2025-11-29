using Olbrasoft.Data.Entities.Abstractions;

namespace VoiceAssistant.Shared.Data.Entities;

/// <summary>
/// Lookup table entity for speech lock sources.
/// </summary>
public class SpeechLockSourceEntity : BaseEnity
{
    /// <summary>
    /// Gets or sets the name of the source.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the source.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Navigation property to speech locks with this source.
    /// </summary>
    public ICollection<SpeechLockEntity> SpeechLocks { get; set; } = [];
}
