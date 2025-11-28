using Olbrasoft.Data.Entities.Abstractions;

namespace VoiceAssistant.Shared.Data.Entities;

/// <summary>
/// Entity representing a TTS voice profile.
/// </summary>
public class VoiceProfile : BaseEnity
{
    /// <summary>
    /// Gets or sets the display name of the voice profile.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the voice identifier (e.g., "cs-CZ-AntoninNeural").
    /// </summary>
    public required string VoiceId { get; set; }

    /// <summary>
    /// Gets or sets the speech rate (e.g., "+0%", "+20%", "-10%").
    /// </summary>
    public string Rate { get; set; } = "+0%";

    /// <summary>
    /// Gets or sets the pitch (e.g., "+0Hz", "+10Hz").
    /// </summary>
    public string Pitch { get; set; } = "+0Hz";

    /// <summary>
    /// Gets or sets the volume (e.g., "+0%", "+50%").
    /// </summary>
    public string Volume { get; set; } = "+0%";

    /// <summary>
    /// Gets or sets whether this is the default profile.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
