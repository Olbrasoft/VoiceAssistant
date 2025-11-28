using Olbrasoft.Data.Cqrs;
using Olbrasoft.Mediation;
using VoiceAssistant.Shared.Data.Enums;

namespace VoiceAssistant.Shared.Data.Commands;

/// <summary>
/// Command to save a transcription log entry.
/// </summary>
public class TranscriptionLogSaveCommand : VoiceAssistantCommand<int>
{
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
    /// Gets or sets the source of the transcription.
    /// </summary>
    public TranscriptionSource Source { get; set; }

    /// <summary>
    /// Gets or sets the language code.
    /// </summary>
    public string? Language { get; set; }

    public TranscriptionLogSaveCommand(ICommandExecutor executor) : base(executor)
    {
    }

    public TranscriptionLogSaveCommand(IMediator mediator) : base(mediator)
    {
    }
    
    public TranscriptionLogSaveCommand()
    {
    }
}
