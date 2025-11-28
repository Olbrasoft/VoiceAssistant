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
}
