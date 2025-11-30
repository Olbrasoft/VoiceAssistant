using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Olbrasoft.VoiceAssistant.Shared.TextInput;

/// <summary>
/// Service for sending text to applications.
/// Supports both OpenCode HTTP API and fallback xdotool typing.
/// </summary>
public class TextInputService
{
    private readonly ILogger<TextInputService> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public TextInputService(ILogger<TextInputService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    }

    /// <summary>
    /// Sends text to OpenCode via HTTP API, falls back to xdotool if OpenCode is not available.
    /// </summary>
    /// <param name="text">Text to send.</param>
    /// <param name="submitPrompt">If true, submits the prompt after sending text (presses Enter).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if text was sent successfully, false otherwise.</returns>
    public async Task<bool> TypeTextAsync(string text, bool submitPrompt = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Cannot send empty text");
            return false;
        }

        // Try OpenCode API first
        var openCodeUrl = _configuration["OpenCodeUrl"] ?? "http://localhost:4096";
        
        var httpResult = await SendToOpenCodeAsync(openCodeUrl, text, submitPrompt, cancellationToken);
        
        if (httpResult)
        {
            return true;
        }

        // Fallback to xdotool
        _logger.LogWarning("⚠️ OpenCode API unavailable, falling back to xdotool");
        return await TypeWithXdotoolAsync(text, submitPrompt, cancellationToken);
    }

    /// <summary>
    /// Sends text to OpenCode via HTTP POST to /tui/append-prompt endpoint.
    /// Optionally submits the prompt with /tui/submit-prompt.
    /// </summary>
    private async Task<bool> SendToOpenCodeAsync(string baseUrl, string text, bool submitPrompt, CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Append text to prompt
            var appendEndpoint = $"{baseUrl.TrimEnd('/')}/tui/append-prompt";
            
            var payload = new { text };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(appendEndpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenCode API returned {StatusCode}", response.StatusCode);
                return false;
            }

            // Step 2: Submit prompt if requested
            if (submitPrompt)
            {
                await Task.Delay(100, cancellationToken); // Small delay to ensure text is appended
                
                var submitEndpoint = $"{baseUrl.TrimEnd('/')}/tui/submit-prompt";
                var submitResponse = await _httpClient.PostAsync(submitEndpoint, null, cancellationToken);

                if (!submitResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to submit prompt: {StatusCode}", submitResponse.StatusCode);
                }
            }

            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "❌ OpenCode API not reachable at {Url} - {Message}", baseUrl, ex.Message);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "❌ OpenCode API timeout at {Url}", baseUrl);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send text to OpenCode: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Types text into the currently focused window using xdotool.
    /// Fallback method when OpenCode API is not available.
    /// </summary>
    private async Task<bool> TypeWithXdotoolAsync(string text, bool submitPrompt, CancellationToken cancellationToken)
    {
        try
        {
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

            // Submit with Enter key if requested
            if (submitPrompt)
            {
                await Task.Delay(100, cancellationToken); // Small delay before pressing Enter
                
                var enterProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = "xdotool",
                    Arguments = "key Return",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (enterProcess != null)
                {
                    await enterProcess.WaitForExitAsync(cancellationToken);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to type text with xdotool");
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
