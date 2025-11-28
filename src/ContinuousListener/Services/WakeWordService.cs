using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Olbrasoft.VoiceAssistant.ContinuousListener.Services;

/// <summary>
/// Service for detecting wake words in transcribed text.
/// </summary>
public class WakeWordService
{
    private readonly ILogger<WakeWordService> _logger;
    private readonly ContinuousListenerOptions _options;

    public WakeWordService(ILogger<WakeWordService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _options = new ContinuousListenerOptions();
        configuration.GetSection(ContinuousListenerOptions.SectionName).Bind(_options);
    }

    /// <summary>
    /// Result of wake word detection.
    /// </summary>
    public record WakeWordResult(bool Detected, string? WakeWord, string? Command, string FullTranscript);

    /// <summary>
    /// Detects wake word in the transcript and extracts the command after it.
    /// </summary>
    /// <param name="transcript">Transcribed text to analyze.</param>
    /// <returns>Detection result with optional command text.</returns>
    public WakeWordResult Detect(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return new WakeWordResult(false, null, null, transcript);
        }

        string lowerText = transcript.ToLowerInvariant();

        foreach (var wakeWord in _options.WakeWords)
        {
            string pattern = $@"\b{Regex.Escape(wakeWord)}\b";
            var match = Regex.Match(lowerText, pattern, RegexOptions.IgnoreCase);
            
            if (match.Success)
            {
                // Extract command after wake word
                int commandStart = match.Index + match.Length;
                string command = transcript.Substring(commandStart).Trim();
                
                // Remove leading punctuation/whitespace from command
                command = Regex.Replace(command, @"^[\s,.:!?]+", "").Trim();

                _logger.LogInformation("Wake word detected: '{WakeWord}', Command: '{Command}'", wakeWord, command);
                
                return new WakeWordResult(true, wakeWord, 
                    string.IsNullOrWhiteSpace(command) ? null : command, 
                    transcript);
            }
        }

        _logger.LogDebug("No wake word detected in: '{Transcript}'", transcript);
        return new WakeWordResult(false, null, null, transcript);
    }

    /// <summary>
    /// Gets the configured wake words.
    /// </summary>
    public IReadOnlyList<string> WakeWords => _options.WakeWords;
}
