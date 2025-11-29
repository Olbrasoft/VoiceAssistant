using Olbrasoft.Data.Entities.Abstractions;

namespace VoiceAssistant.Shared.Data.Entities;

/// <summary>
/// Entity representing a speech lock to prevent AI from speaking while user is recording.
/// </summary>
public class SpeechLockEntity : BaseEnity
{
    /// <summary>
    /// Gets or sets the timestamp when the lock was created.
    /// This is set automatically by the database.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the source ID (foreign key to SpeechLockSources).
    /// </summary>
    public int SourceId { get; set; }

    /// <summary>
    /// Navigation property to the source.
    /// </summary>
    public SpeechLockSourceEntity? Source { get; set; }

    /// <summary>
    /// Gets or sets optional reason for the lock (e.g., "WakeWord:OpenCode", "Recording").
    /// </summary>
    public string? Reason { get; set; }
}
