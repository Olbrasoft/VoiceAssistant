using System.Diagnostics;

namespace Olbrasoft.VoiceAssistant.Orchestration.Services;

/// <summary>
/// Service for typing text into the focused window using xdotool.
/// </summary>
public class TextInputService
{
    private readonly ILogger<TextInputService> _logger;

    public TextInputService(ILogger<TextInputService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Types text into the currently focused window.
    /// Uses xdotool to simulate keyboard input.
    /// </summary>
    /// <param name="text">Text to type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if typing succeeded, false otherwise.</returns>
    public async Task<bool> TypeTextAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Cannot type empty text");
            return false;
        }

        try
        {
            _logger.LogInformation("⌨️  Typing text: {Text}", text);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "xdotool",
                Arguments = $"type --clearmodifiers \"{EscapeForXdotool(text)}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
            {
                _logger.LogError("❌ Failed to start xdotool process");
                return false;
            }

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(cancellationToken);
                _logger.LogError("❌ xdotool failed: {Error}", error);
                return false;
            }

            _logger.LogInformation("✅ Text typed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to type text");
            return false;
        }
    }

    /// <summary>
    /// Escapes text for xdotool command line.
    /// Handles quotes and special characters.
    /// </summary>
    private static string EscapeForXdotool(string text)
    {
        // Escape double quotes and backslashes for shell
        return text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }
}
