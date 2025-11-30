using Microsoft.EntityFrameworkCore;
using VoiceAssistant.Data.EntityFrameworkCore;
using VoiceAssistant.Shared.Data.Entities;
using VoiceAssistant.Shared.Data.Enums;

namespace Olbrasoft.VoiceAssistant.ContinuousListener.Services;

/// <summary>
/// Service for managing speech locks to prevent TTS from speaking while user is recording.
/// Locks are stored in database with automatic expiration after timeout.
/// </summary>
public class SpeechLockService
{
    private const int LockTimeoutSeconds = 30;
    
    private readonly ILogger<SpeechLockService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private int? _currentLockId;

    public SpeechLockService(ILogger<SpeechLockService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Acquires a speech lock to prevent TTS from speaking.
    /// </summary>
    public async Task<bool> LockAsync(string? reason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<VoiceAssistantDbContext>();

            var lockEntity = new SpeechLockEntity 
            { 
                CreatedAt = DateTime.UtcNow,
                SourceId = (int)SpeechLockSource.ContinuousListener,
                Reason = reason
            };
            db.SpeechLocks.Add(lockEntity);
            await db.SaveChangesAsync(cancellationToken);

            _currentLockId = lockEntity.Id;
            _logger.LogInformation("🔒 Speech lock acquired (ID: {LockId}, Reason: {Reason})", _currentLockId, reason);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire speech lock");
            return false;
        }
    }

    /// <summary>
    /// Releases the current speech lock.
    /// </summary>
    public async Task<bool> UnlockAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_currentLockId == null)
            {
                _logger.LogDebug("No lock to release");
                return true;
            }

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<VoiceAssistantDbContext>();

            var lockEntity = await db.SpeechLocks.FindAsync([_currentLockId], cancellationToken);
            if (lockEntity != null)
            {
                db.SpeechLocks.Remove(lockEntity);
                await db.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation("🔓 Speech lock released (ID: {LockId})", _currentLockId);
            _currentLockId = null;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release speech lock");
            return false;
        }
    }

    /// <summary>
    /// Checks if there is any active (non-expired) speech lock.
    /// Lock is valid only if CreatedAt + timeout > now.
    /// </summary>
    public async Task<bool> IsLockedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<VoiceAssistantDbContext>();

            var cutoff = DateTime.UtcNow.AddSeconds(-LockTimeoutSeconds);
            
            // Check if any valid (non-expired) lock exists
            return await db.SpeechLocks
                .AnyAsync(l => l.CreatedAt > cutoff, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check speech lock status");
            return false;
        }
    }

    /// <summary>
    /// Gets all active (non-expired) speech locks.
    /// </summary>
    public async Task<List<SpeechLockEntity>> GetActiveLocksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<VoiceAssistantDbContext>();

            var cutoff = DateTime.UtcNow.AddSeconds(-LockTimeoutSeconds);
            
            return await db.SpeechLocks
                .Where(l => l.CreatedAt > cutoff)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active locks");
            return new List<SpeechLockEntity>();
        }
    }

    /// <summary>
    /// Cleans up all expired locks from database.
    /// </summary>
    public async Task CleanupExpiredLocksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<VoiceAssistantDbContext>();

            var cutoff = DateTime.UtcNow.AddSeconds(-LockTimeoutSeconds);
            
            var expiredLocks = await db.SpeechLocks
                .Where(l => l.CreatedAt <= cutoff)
                .ToListAsync(cancellationToken);

            if (expiredLocks.Count > 0)
            {
                db.SpeechLocks.RemoveRange(expiredLocks);
                await db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("🧹 Cleaned up {Count} expired speech locks", expiredLocks.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired locks");
        }
    }
}
