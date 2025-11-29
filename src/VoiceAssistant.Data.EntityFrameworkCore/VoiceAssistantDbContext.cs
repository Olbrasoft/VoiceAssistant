using Microsoft.EntityFrameworkCore;
using VoiceAssistant.Shared.Data.Entities;
using VoiceAssistant.Shared.Data.Enums;
using VoiceAssistant.Shared.Extensions;

namespace VoiceAssistant.Data.EntityFrameworkCore;

/// <summary>
/// Database context for VoiceAssistant.
/// </summary>
public class VoiceAssistantDbContext : DbContext
{
    /// <summary>
    /// Gets or sets the transcription logs.
    /// </summary>
    public DbSet<TranscriptionLog> TranscriptionLogs => Set<TranscriptionLog>();

    /// <summary>
    /// Gets or sets the transcription sources (lookup table).
    /// </summary>
    public DbSet<TranscriptionSourceEntity> TranscriptionSources => Set<TranscriptionSourceEntity>();

    /// <summary>
    /// Gets or sets the settings.
    /// </summary>
    public DbSet<Setting> Settings => Set<Setting>();

    /// <summary>
    /// Gets or sets the voice profiles.
    /// </summary>
    public DbSet<VoiceProfile> VoiceProfiles => Set<VoiceProfile>();

    /// <summary>
    /// Gets or sets the speech locks.
    /// </summary>
    public DbSet<SpeechLockEntity> SpeechLocks => Set<SpeechLockEntity>();

    /// <summary>
    /// Gets or sets the speech lock sources (lookup table).
    /// </summary>
    public DbSet<SpeechLockSourceEntity> SpeechLockSources => Set<SpeechLockSourceEntity>();

    public VoiceAssistantDbContext(DbContextOptions<VoiceAssistantDbContext> options) 
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TranscriptionSource configuration (lookup table)
        modelBuilder.Entity<TranscriptionSourceEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.HasIndex(e => e.Name).IsUnique();

            // Seed data from enum - single source of truth
            entity.HasData(
                Enum.GetValues<TranscriptionSource>()
                    .Select(e => new TranscriptionSourceEntity
                    {
                        Id = (int)e,
                        Name = e.ToString(),
                        Description = e.GetDescription()
                    })
                    .ToArray()
            );
        });

        // TranscriptionLog configuration
        modelBuilder.Entity<TranscriptionLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Text).IsRequired();
            entity.Property(e => e.Language).HasMaxLength(10);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.SourceId);

            // Foreign key to TranscriptionSource
            entity.HasOne(e => e.Source)
                  .WithMany(s => s.TranscriptionLogs)
                  .HasForeignKey(e => e.SourceId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Setting configuration
        modelBuilder.Entity<Setting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Value).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.HasIndex(e => e.Key).IsUnique();
        });

        // VoiceProfile configuration
        modelBuilder.Entity<VoiceProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.VoiceId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Rate).HasMaxLength(20);
            entity.Property(e => e.Pitch).HasMaxLength(20);
            entity.Property(e => e.Volume).HasMaxLength(20);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // SpeechLockSource configuration (lookup table)
        modelBuilder.Entity<SpeechLockSourceEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.HasIndex(e => e.Name).IsUnique();

            // Seed data from enum - single source of truth
            entity.HasData(
                Enum.GetValues<SpeechLockSource>()
                    .Select(e => new SpeechLockSourceEntity
                    {
                        Id = (int)e,
                        Name = e.ToString(),
                        Description = e.GetDescription()
                    })
                    .ToArray()
            );
        });

        // SpeechLockEntity configuration
        modelBuilder.Entity<SpeechLockEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("datetime('now')")
                .ValueGeneratedOnAdd();
            entity.Property(e => e.Reason).HasMaxLength(100);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.SourceId);

            // Foreign key to SpeechLockSource
            entity.HasOne(e => e.Source)
                  .WithMany(s => s.SpeechLocks)
                  .HasForeignKey(e => e.SourceId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
