using VoiceAssistant.Shared.Data.Commands;
using VoiceAssistant.Shared.Data.Entities;

namespace VoiceAssistant.Data.EntityFrameworkCore.CommandHandlers;

/// <summary>
/// Handler for saving transcription log entries.
/// </summary>
public class TranscriptionLogSaveCommandHandler 
    : VoiceAssistantDbCommandHandler<TranscriptionLogSaveCommand, TranscriptionLog, int>
{
    public TranscriptionLogSaveCommandHandler(VoiceAssistantDbContext context) : base(context)
    {
    }

    protected override async Task<int> GetResultToHandleAsync(
        TranscriptionLogSaveCommand command, 
        CancellationToken token)
    {
        var entity = new TranscriptionLog
        {
            Text = command.Text,
            Confidence = command.Confidence,
            DurationMs = command.DurationMs,
            SourceId = (int)command.Source,  // Enum value maps directly to FK
            Language = command.Language,
            CreatedAt = DateTime.UtcNow
        };

        if (command.Id > 0)
        {
            entity.Id = command.Id;
            await UpdateAsync(entity, token);
        }
        else
        {
            await InsertAsync(entity, token);
        }

        return entity.Id;
    }
}
