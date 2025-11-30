using Microsoft.EntityFrameworkCore;
using VoiceAssistant.Data.EntityFrameworkCore;

namespace Olbrasoft.VoiceAssistant.ContinuousListener.Services;

/// <summary>
/// Service for tracking assistant speech state in the database.
/// Used to prevent processing audio when the assistant is speaking (echo suppression).
/// </summary>
public class AssistantSpeechStateService
{
    private readonly ILogger<AssistantSpeechStateService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private const int StateRecordId = 1; // Singleton record
    private const int MaxSpeakingDurationSeconds = 30; // Consider stale after 30 seconds

    public AssistantSpeechStateService(
        ILogger<AssistantSpeechStateService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Checks if the assistant is currently speaking.
    /// Returns false if the speaking state is older than 30 seconds (considered stale).
    /// </summary>
    /// <returns>True if assistant is actively speaking, false otherwise.</returns>
    public async Task<bool> IsAssistantSpeakingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<VoiceAssistantDbContext>();

            var state = await dbContext.AssistantSpeechStates
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == StateRecordId, cancellationToken);

            if (state == null || !state.IsSpeaking)
            {
                return false;
            }

            // Check if the speaking state is stale (older than 30 seconds)
            if (state.StartedAt.HasValue)
            {
                var speakingDuration = DateTime.UtcNow - state.StartedAt.Value;
                if (speakingDuration.TotalSeconds > MaxSpeakingDurationSeconds)
                {
                    _logger.LogWarning("🔇 Assistant speech state is stale ({Duration}s > {Max}s), clearing", 
                        speakingDuration.TotalSeconds, MaxSpeakingDurationSeconds);
                    
                    // Clear the stale state
                    await ClearStaleSpeechStateAsync(cancellationToken);
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check assistant speech state, assuming not speaking");
            return false;
        }
    }

    /// <summary>
    /// Clears a stale speech state (when IsSpeaking has been true for too long).
    /// </summary>
    private async Task ClearStaleSpeechStateAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<VoiceAssistantDbContext>();

            var state = await dbContext.AssistantSpeechStates
                .FirstOrDefaultAsync(s => s.Id == StateRecordId, cancellationToken);

            if (state != null && state.IsSpeaking)
            {
                state.IsSpeaking = false;
                state.EndedAt = DateTime.UtcNow;
                state.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("🧹 Cleared stale assistant speech state");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear stale speech state");
        }
    }
}
