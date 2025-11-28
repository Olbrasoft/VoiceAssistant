using VoiceAssistant.Shared.Data.Commands.SpeechLockCommands;
using VoiceAssistant.Shared.Data.Entities;

namespace VoiceAssistant.Data.EntityFrameworkCore.CommandHandlers.SpeechLockCommandHandlers;

/// <summary>
/// Handler for creating a speech lock.
/// </summary>
public class SpeechLockCreateCommandHandler 
    : VoiceAssistantDbCommandHandler<SpeechLockCreateCommand, SpeechLockEntity, int>
{
    public SpeechLockCreateCommandHandler(VoiceAssistantDbContext context) : base(context)
    {
    }

    protected override async Task<int> GetResultToHandleAsync(
        SpeechLockCreateCommand command, 
        CancellationToken token)
    {
        // First, delete any existing locks (there should be only one active lock at a time)
        var existingLocks = Context.SpeechLocks.ToList();
        if (existingLocks.Count > 0)
        {
            Context.SpeechLocks.RemoveRange(existingLocks);
        }

        // Create new lock - CreatedAt is set by database default
        var entity = new SpeechLockEntity();
        
        await InsertAsync(entity, token);

        return entity.Id;
    }
}
