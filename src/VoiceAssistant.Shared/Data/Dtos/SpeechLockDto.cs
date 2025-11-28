namespace VoiceAssistant.Shared.Data.Dtos;

/// <summary>
/// Data transfer object for SpeechLock.
/// </summary>
public class SpeechLockDto
{
    /// <summary>
    /// Gets or sets the lock identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the lock was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
