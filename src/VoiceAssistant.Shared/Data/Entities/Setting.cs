using Olbrasoft.Data.Entities.Abstractions;

namespace VoiceAssistant.Shared.Data.Entities;

/// <summary>
/// Entity representing application settings stored in the database.
/// </summary>
public class Setting : BaseEnity
{
    /// <summary>
    /// Gets or sets the setting key.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// Gets or sets the setting value as JSON.
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// Gets or sets the setting category (e.g., "Audio", "Speech", "TTS").
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the description of the setting.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the last modification timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
