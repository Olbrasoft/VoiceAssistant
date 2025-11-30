using Olbrasoft.Data.Entities.Abstractions;
using VoiceAssistant.Shared.Data.Enums;

namespace VoiceAssistant.Shared.Data.Entities;

/// <summary>
/// Entity representing a Groq Router decision log entry.
/// Logs every decision made by Groq about how to handle voice input.
/// </summary>
public class GroqRouterLog : BaseEnity
{
    /// <summary>
    /// Gets or sets the optional reference to the original transcription log.
    /// </summary>
    public int? TranscriptionLogId { get; set; }

    /// <summary>
    /// Navigation property to the original transcription.
    /// </summary>
    public TranscriptionLog? TranscriptionLog { get; set; }

    /// <summary>
    /// Gets or sets the input text that was sent to Groq.
    /// </summary>
    public required string InputText { get; set; }

    /// <summary>
    /// Gets or sets the action determined by Groq.
    /// </summary>
    public GroqRouterAction Action { get; set; }

    /// <summary>
    /// Gets or sets the confidence score (0.0 to 1.0).
    /// </summary>
    public float Confidence { get; set; }

    /// <summary>
    /// Gets or sets the reason provided by Groq for the decision.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the direct response text (when Action = Respond).
    /// This is the text that should be played via TTS.
    /// </summary>
    public string? Response { get; set; }

    /// <summary>
    /// Gets or sets the command for OpenCode (when Action = OpenCode).
    /// This is the summarized command to send to OpenCode.
    /// </summary>
    public string? CommandForOpenCode { get; set; }

    /// <summary>
    /// Gets or sets the Groq API response time in milliseconds.
    /// </summary>
    public int ResponseTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the routing decision was made.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether the response was successfully processed
    /// (TTS played or command dispatched).
    /// </summary>
    public bool WasProcessed { get; set; }

    /// <summary>
    /// Gets or sets optional error message if processing failed.
    /// </summary>
    public string? ProcessingError { get; set; }
}
