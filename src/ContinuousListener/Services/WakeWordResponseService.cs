using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Olbrasoft.VoiceAssistant.ContinuousListener.Services;

/// <summary>
/// Service for playing audio responses when wake words are detected.
/// Supports different response sets for different wake words.
/// </summary>
public class WakeWordResponseService
{
    private readonly ILogger<WakeWordResponseService> _logger;
    private readonly ContinuousListenerOptions _options;
    private readonly Random _random = new();
    private readonly Dictionary<string, string[]> _responsePaths = new();

    public WakeWordResponseService(ILogger<WakeWordResponseService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _options = new ContinuousListenerOptions();
        configuration.GetSection(ContinuousListenerOptions.SectionName).Bind(_options);
        
        LoadResponseFiles();
    }

    private void LoadResponseFiles()
    {
        // Load "počítači" responses (deep voice)
        LoadResponsesForWakeWord(
            _options.ComputerResponsesPath, 
            "computer_response_*.mp3", 
            "počítači");

        // Load "opencode" responses (normal voice)
        LoadResponsesForWakeWord(
            _options.OpenCodeResponsesPath, 
            "opencode_response_*.mp3", 
            "opencode");
        
        // Map "open code" to same responses as "opencode"
        if (_responsePaths.ContainsKey("opencode"))
        {
            _responsePaths["open code"] = _responsePaths["opencode"];
        }
    }

    private void LoadResponsesForWakeWord(string directory, string pattern, string wakeWord)
    {
        if (Directory.Exists(directory))
        {
            var files = Directory.GetFiles(directory, pattern)
                .OrderBy(f => f)
                .ToArray();
            
            if (files.Length > 0)
            {
                _responsePaths[wakeWord] = files;
                _logger.LogInformation("Loaded {Count} audio responses for '{WakeWord}' from {Path}", 
                    files.Length, wakeWord, directory);
            }
            else
            {
                _logger.LogWarning("No {Pattern} files found in {Path}", pattern, directory);
            }
        }
        else
        {
            _logger.LogWarning("Responses directory not found: {Path}", directory);
        }
    }

    /// <summary>
    /// Plays a random audio response for the detected wake word.
    /// </summary>
    /// <param name="wakeWord">The detected wake word (lowercase).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if audio was played successfully.</returns>
    public async Task<bool> PlayResponseAsync(string wakeWord, CancellationToken cancellationToken = default)
    {
        var wakeWordLower = wakeWord.ToLowerInvariant();
        
        if (!_responsePaths.TryGetValue(wakeWordLower, out var files) || files.Length == 0)
        {
            _logger.LogDebug("No audio responses configured for wake word: '{WakeWord}'", wakeWord);
            return false;
        }

        // Pick random response
        var selectedFile = files[_random.Next(files.Length)];
        
        _logger.LogInformation("Playing audio response: {File}", Path.GetFileName(selectedFile));

        try
        {
            // Use ffplay for playback (available on most Linux systems)
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffplay",
                    Arguments = $"-nodisp -autoexit -loglevel quiet \"{selectedFile}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0)
            {
                _logger.LogDebug("Audio playback completed successfully");
                return true;
            }
            else
            {
                var error = await process.StandardError.ReadToEndAsync(cancellationToken);
                _logger.LogWarning("Audio playback failed with exit code {ExitCode}: {Error}", 
                    process.ExitCode, error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error playing audio response");
            return false;
        }
    }

    /// <summary>
    /// Checks if the wake word has audio responses configured.
    /// </summary>
    /// <param name="wakeWord">The wake word to check.</param>
    /// <returns>True if audio responses are available.</returns>
    public bool HasResponses(string wakeWord)
    {
        var wakeWordLower = wakeWord.ToLowerInvariant();
        return _responsePaths.TryGetValue(wakeWordLower, out var files) && files.Length > 0;
    }
}
