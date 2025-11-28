using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Olbrasoft.Mediation;
using VoiceAssistant.Data.EntityFrameworkCore.CommandHandlers;
using VoiceAssistant.Shared.Data.Commands;

namespace VoiceAssistant.Data.EntityFrameworkCore;

/// <summary>
/// Extension methods for registering VoiceAssistant data services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds VoiceAssistant data services including EF Core DbContext and CQRS handlers.
    /// </summary>
    public static IServiceCollection AddVoiceAssistantData(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Get database path from configuration
        var dbPath = configuration.GetValue<string>("VoiceAssistant:DatabasePath") 
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "voice-assistant",
                "voice-assistant.db");

        // Expand ~ if present
        if (dbPath.StartsWith("~/"))
        {
            dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                dbPath[2..]);
        }

        // Register DbContext
        services.AddDbContext<VoiceAssistantDbContext>(options =>
        {
            options.UseSqlite($"Data Source={dbPath}");
        });

        // Register Mediation with assembly containing handlers
        var handlerAssembly = typeof(TranscriptionLogSaveCommandHandler).Assembly;
        services.AddMediation(handlerAssembly).UseRequestHandlerMediator();

        return services;
    }
}
