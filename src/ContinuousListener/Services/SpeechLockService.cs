using Microsoft.EntityFrameworkCore;
using VoiceAssistant.Data.EntityFrameworkCore;
using VoiceAssistant.Shared.Data.Entities;
using VoiceAssistant.Shared.Data.Enums;

namespace Olbrasoft.VoiceAssistant.ContinuousListener.Services;

/// <summary>
/// Service for managing speech locks to prevent TTS from speaking while user is recording.
/// Uses both database (for future extensibility) and file lock (for bash script compatibility).
/// </summary>
public class SpeechLockService
{
    private const string FileLockPath = "/tmp/speech-lock";
    
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
    /// <param name="reason">Optional reason for the lock.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<bool> LockAsync(string? reason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create file lock for bash script compatibility
            await File.WriteAllTextAsync(FileLockPath, $"ContinuousListener:{reason ?? "recording"}", cancellationToken);
            
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
            // Remove file lock
            if (File.Exists(FileLockPath))
            {
                File.Delete(FileLockPath);
            }
            
            if (_currentLockId == null)
            {
                _logger.LogDebug("No DB lock to release");
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
    /// Cleans up any stale locks (older than 30 seconds).
    /// </summary>
    public async Task CleanupStaleLocks(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<VoiceAssistantDbContext>();

            var cutoff = DateTime.UtcNow.AddSeconds(-30);
            var staleLocks = await db.SpeechLocks
                .Where(l => l.CreatedAt < cutoff)
                .ToListAsync(cancellationToken);

            if (staleLocks.Count > 0)
            {
                db.SpeechLocks.RemoveRange(staleLocks);
                await db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("🧹 Cleaned up {Count} stale speech locks", staleLocks.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup stale locks");
        }
    }
}
