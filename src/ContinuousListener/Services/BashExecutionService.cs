using System.Diagnostics;

namespace Olbrasoft.VoiceAssistant.ContinuousListener.Services;

/// <summary>
/// Service for executing bash commands from voice input.
/// Commands are run in background to not block the ContinuousListener.
/// </summary>
public class BashExecutionService
{
    private readonly ILogger<BashExecutionService> _logger;

    public BashExecutionService(ILogger<BashExecutionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executes a bash command in background (non-blocking).
    /// </summary>
    /// <param name="command">The bash command to execute.</param>
    /// <returns>True if the command was started successfully, false otherwise.</returns>
    public async Task<bool> ExecuteAsync(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            _logger.LogWarning("Empty bash command received");
            return false;
        }

        try
        {
            _logger.LogInformation("Executing bash command: {Command}", command);

            // Wrap command to run in background with nohup
            // This ensures the command continues even if ContinuousListener stops
            var wrappedCommand = $"nohup {command} &>/dev/null &";

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{EscapeForBash(wrappedCommand)}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();

            // Wait a short time for the bash wrapper to complete
            // (the actual command continues in background)
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                _logger.LogInformation("Bash command started successfully: {Command}", command);
                return true;
            }
            else
            {
                var stderr = await process.StandardError.ReadToEndAsync();
                _logger.LogWarning("Bash command failed with exit code {ExitCode}: {Error}", 
                    process.ExitCode, stderr);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute bash command: {Command}", command);
            return false;
        }
    }

    /// <summary>
    /// Escapes a string for safe use in bash command.
    /// </summary>
    private static string EscapeForBash(string input)
    {
        // Escape double quotes and backslashes
        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }
}
