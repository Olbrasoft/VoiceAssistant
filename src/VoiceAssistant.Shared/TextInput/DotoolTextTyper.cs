using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Olbrasoft.VoiceAssistant.Shared.TextInput;

/// <summary>
/// Text typer implementation using clipboard + dotool for Linux Wayland.
/// Saves clipboard content, pastes text, then restores original clipboard.
/// This approach supports full Unicode including Czech diacritics (háčky, čárky).
/// </summary>
public class DotoolTextTyper : ITextTyper
{
    private readonly ILogger<DotoolTextTyper> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DotoolTextTyper"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public DotoolTextTyper(ILogger<DotoolTextTyper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public bool IsAvailable
    {
        get
        {
            try
            {
                // Check both dotool and wl-copy are available
                var dotoolCheck = Process.Start(new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "dotool",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                dotoolCheck?.WaitForExit(1000);
                
                var wlCopyCheck = Process.Start(new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "wl-copy",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                wlCopyCheck?.WaitForExit(1000);
                
                return dotoolCheck?.ExitCode == 0 && wlCopyCheck?.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <inheritdoc/>
    public async Task TypeTextAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Attempted to type empty or whitespace text");
            return;
        }

        if (!IsAvailable)
        {
            _logger.LogError("dotool or wl-copy is not available on this system");
            throw new InvalidOperationException("dotool or wl-copy is not available");
        }

        try
        {
            // Convert to lowercase and add space
            var textToType = text.ToLower() + " ";

            // Step 1: Save current clipboard content
            string? originalClipboard = null;
            try
            {
                var wlPasteProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "wl-paste",
                        Arguments = "--no-newline",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                wlPasteProcess.Start();
                originalClipboard = await wlPasteProcess.StandardOutput.ReadToEndAsync(cancellationToken);
                await wlPasteProcess.WaitForExitAsync(cancellationToken);
                _logger.LogDebug("Saved original clipboard content ({Length} chars)", originalClipboard?.Length ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Could not save clipboard: {Message}", ex.Message);
                // Continue anyway - clipboard might be empty
            }

            // Step 2: Copy our text to clipboard
            var wlCopyProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "wl-copy",
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            wlCopyProcess.Start();
            await wlCopyProcess.StandardInput.WriteAsync(textToType);
            wlCopyProcess.StandardInput.Close();
            await wlCopyProcess.WaitForExitAsync(cancellationToken);

            if (wlCopyProcess.ExitCode != 0)
            {
                var error = await wlCopyProcess.StandardError.ReadToEndAsync(cancellationToken);
                _logger.LogError("wl-copy failed with exit code {ExitCode}: {Error}", wlCopyProcess.ExitCode, error);
                throw new InvalidOperationException($"wl-copy failed: {error}");
            }

            // Small delay to ensure clipboard is ready
            await Task.Delay(50, cancellationToken);

            // Step 3: Simulate paste - send both Ctrl+Shift+V (terminal) and Ctrl+V (GUI apps)
            // Terminal will respond to Ctrl+Shift+V, GUI apps will respond to Ctrl+V
            // The other shortcut is ignored by each type of application
            var dotoolProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = "-c \"echo -e 'key ctrl+shift+v\\nkey ctrl+v' | dotool\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            dotoolProcess.Start();
            await dotoolProcess.WaitForExitAsync(cancellationToken);

            if (dotoolProcess.ExitCode != 0)
            {
                var error = await dotoolProcess.StandardError.ReadToEndAsync(cancellationToken);
                _logger.LogError("dotool failed with exit code {ExitCode}: {Error}", dotoolProcess.ExitCode, error);
                throw new InvalidOperationException($"dotool failed: {error}");
            }

            // Small delay to ensure paste completed
            await Task.Delay(100, cancellationToken);

            // Step 4: Restore original clipboard content
            if (!string.IsNullOrEmpty(originalClipboard))
            {
                try
                {
                    var restoreProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "wl-copy",
                            RedirectStandardInput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    restoreProcess.Start();
                    await restoreProcess.StandardInput.WriteAsync(originalClipboard);
                    restoreProcess.StandardInput.Close();
                    await restoreProcess.WaitForExitAsync(cancellationToken);
                    _logger.LogDebug("Restored original clipboard content");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Could not restore clipboard: {Message}", ex.Message);
                }
            }

            _logger.LogDebug("Successfully typed {CharCount} characters", textToType.Length);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Text typing was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to type text: {Text}", text);
            throw;
        }
    }
}
