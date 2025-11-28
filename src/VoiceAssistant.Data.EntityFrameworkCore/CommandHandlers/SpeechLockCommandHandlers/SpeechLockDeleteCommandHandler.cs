using Microsoft.EntityFrameworkCore;
using VoiceAssistant.Shared.Data.Commands.SpeechLockCommands;
using VoiceAssistant.Shared.Data.Entities;

namespace VoiceAssistant.Data.EntityFrameworkCore.CommandHandlers.SpeechLockCommandHandlers;

/// <summary>
/// Handler for deleting a speech lock.
/// </summary>
public class SpeechLockDeleteCommandHandler 
    : VoiceAssistantDbCommandHandler<SpeechLockDeleteCommand, SpeechLockEntity>
{
    public SpeechLockDeleteCommandHandler(VoiceAssistantDbContext context) : base(context)
    {
    }

    protected override async Task<bool> GetResultToHandleAsync(
        SpeechLockDeleteCommand command, 
        CancellationToken token)
    {
        // If Id is specified, delete that specific lock
        if (command.Id > 0)
        {
            var entity = await Context.SpeechLocks
                .FirstOrDefaultAsync(e => e.Id == command.Id, token);
            
            if (entity != null)
            {
                Context.SpeechLocks.Remove(entity);
                await Context.SaveChangesAsync(token);
                return true;
            }
            return false;
        }

        // Otherwise, delete all locks (cleanup)
        var allLocks = await Context.SpeechLocks.ToListAsync(token);
        if (allLocks.Count > 0)
        {
            Context.SpeechLocks.RemoveRange(allLocks);
            await Context.SaveChangesAsync(token);
            return true;
        }

        return false;
    }
}
