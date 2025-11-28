using Olbrasoft.Data.Entities.Abstractions;
using VoiceAssistant.Shared.Data.Enums;

namespace VoiceAssistant.Shared.Data.Entities;

/// <summary>
/// Entity representing a speech transcription log entry.
/// </summary>
public class TranscriptionLog : BaseEnity
{
    /// <summary>
    /// Gets or sets the transcribed text.
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// Gets or sets the confidence score (0.0 to 1.0).
    /// </summary>
    public float Confidence { get; set; }

    /// <summary>
    /// Gets or sets the duration of the audio in milliseconds.
    /// </summary>
    public int DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the transcription occurred.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the source ID (foreign key to TranscriptionSourceEntity).
    /// </summary>
    public int SourceId { get; set; }

    /// <summary>
    /// Navigation property for the source.
    /// </summary>
    public TranscriptionSourceEntity? Source { get; set; }

    /// <summary>
    /// Gets or sets the language code (e.g., "cs", "en").
    /// </summary>
    public string? Language { get; set; }
}
