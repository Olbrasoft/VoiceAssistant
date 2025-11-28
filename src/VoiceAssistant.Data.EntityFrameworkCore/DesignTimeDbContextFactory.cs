using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VoiceAssistant.Data.EntityFrameworkCore;

/// <summary>
/// Design-time factory for creating VoiceAssistantDbContext during migrations.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<VoiceAssistantDbContext>
{
    public VoiceAssistantDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<VoiceAssistantDbContext>();
        
        // Use SQLite for design-time migrations - use production path
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "voice-assistant",
            "voice-assistant.db");
        var connectionString = $"Data Source={dbPath}";
        optionsBuilder.UseSqlite(connectionString);

        return new VoiceAssistantDbContext(optionsBuilder.Options);
    }
}
