using VoiceAssistant.Shared.Data.Enums;

namespace VoiceAssistant.Shared.Data.Dtos;

/// <summary>
/// Data transfer object for transcription log.
/// </summary>
public class TranscriptionLogDto
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the transcribed text.
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// Gets or sets the confidence score.
    /// </summary>
    public float Confidence { get; set; }

    /// <summary>
    /// Gets or sets the duration in milliseconds.
    /// </summary>
    public int DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the transcription occurred.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the source of the transcription.
    /// </summary>
    public TranscriptionSource Source { get; set; }

    /// <summary>
    /// Gets or sets the language code.
    /// </summary>
    public string? Language { get; set; }
}
