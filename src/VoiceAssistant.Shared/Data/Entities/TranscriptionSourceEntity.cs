using Olbrasoft.Data.Entities.Abstractions;

namespace VoiceAssistant.Shared.Data.Entities;

/// <summary>
/// Lookup entity representing a transcription source.
/// This table is seeded from TranscriptionSource enum.
/// </summary>
public class TranscriptionSourceEntity : BaseEnity
{
    /// <summary>
    /// Gets or sets the name of the source (matches enum name).
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the source.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Navigation property for transcription logs with this source.
    /// </summary>
    public ICollection<TranscriptionLog> TranscriptionLogs { get; set; } = [];
}
