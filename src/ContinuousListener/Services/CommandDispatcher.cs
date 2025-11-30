using Olbrasoft.VoiceAssistant.Shared.TextInput;

namespace Olbrasoft.VoiceAssistant.ContinuousListener.Services;

/// <summary>
/// Service for dispatching detected commands to OpenCode or other targets.
/// </summary>
public class CommandDispatcher
{
    private readonly ILogger<CommandDispatcher> _logger;
    private readonly TextInputService _textInputService;

    public CommandDispatcher(ILogger<CommandDispatcher> logger, IConfiguration configuration)
    {
        _logger = logger;
        
        // Create logger for TextInputService
        var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var textInputLogger = loggerFactory.CreateLogger<TextInputService>();
        
        _textInputService = new TextInputService(textInputLogger, configuration);
    }

    /// <summary>
    /// Dispatches a voice command directly to OpenCode session with specified agent.
    /// Uses Session API for proper agent switching (plan/build).
    /// </summary>
    /// <param name="command">Command text to dispatch.</param>
    /// <param name="agent">Agent to use: "plan" for questions, "build" for commands.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if command was dispatched successfully.</returns>
    public async Task<bool> DispatchToSessionAsync(string command, string agent = "build", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            _logger.LogWarning("Cannot dispatch empty command");
            return false;
        }

        _logger.LogInformation("Dispatching to session: '{Command}' (agent: {Agent})", command, agent);

        try
        {
            // Add mode prefix so the LLM knows which mode it's operating in
            var modePrefix = agent == "plan" ? "[PLAN MODE - READ ONLY] " : "[BUILD MODE] ";
            var messageWithPrefix = modePrefix + command;
            
            var result = await _textInputService.SendMessageToSessionAsync(messageWithPrefix, agent, cancellationToken);
            
            if (result)
            {
                _logger.LogInformation("Command dispatched to session successfully");
            }
            else
            {
                _logger.LogWarning("Session dispatch failed");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching to session");
            return false;
        }
    }

    /// <summary>
    /// Dispatches a voice command to the appropriate target via TUI API.
    /// </summary>
    /// <param name="command">Command text to dispatch.</param>
    /// <param name="submitPrompt">Whether to submit the prompt after typing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if command was dispatched successfully.</returns>
    public async Task<bool> DispatchAsync(string command, bool submitPrompt = true, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            _logger.LogWarning("Cannot dispatch empty command");
            return false;
        }

        _logger.LogInformation("Dispatching command: '{Command}' (submit: {Submit})", command, submitPrompt);

        try
        {
            var result = await _textInputService.TypeTextAsync(command, submitPrompt, cancellationToken);
            
            if (result)
            {
                _logger.LogInformation("Command dispatched successfully");
            }
            else
            {
                _logger.LogWarning("Command dispatch failed");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching command");
            return false;
        }
    }
}
