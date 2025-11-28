using Microsoft.EntityFrameworkCore;
using VoiceAssistant.Shared.Data.Entities;
using VoiceAssistant.Shared.Data.Queries.SpeechLockQueries;

namespace VoiceAssistant.Data.EntityFrameworkCore.QueryHandlers.SpeechLockQueryHandlers;

/// <summary>
/// Handler for checking if an active speech lock exists.
/// </summary>
public class SpeechLockExistsQueryHandler 
    : VoiceAssistantDbQueryHandler<SpeechLockEntity, SpeechLockExistsQuery>
{
    public SpeechLockExistsQueryHandler(VoiceAssistantDbContext context) : base(context)
    {
    }

    protected override async Task<bool> GetResultToHandleAsync(
        SpeechLockExistsQuery query, 
        CancellationToken token)
    {
        var cutoffTime = DateTime.UtcNow.AddMinutes(-query.MaxAgeMinutes);
        
        // Check if any lock exists that is newer than the cutoff time
        var exists = await Context.SpeechLocks
            .AnyAsync(e => e.CreatedAt >= cutoffTime, token);

        // Cleanup: delete old locks asynchronously (older than MaxAgeMinutes)
        if (!exists)
        {
            var oldLocks = await Context.SpeechLocks
                .Where(e => e.CreatedAt < cutoffTime)
                .ToListAsync(token);

            if (oldLocks.Count > 0)
            {
                Context.SpeechLocks.RemoveRange(oldLocks);
                await Context.SaveChangesAsync(token);
            }
        }

        return exists;
    }
}
