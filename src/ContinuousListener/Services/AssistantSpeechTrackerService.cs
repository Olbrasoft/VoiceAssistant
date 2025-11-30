using System.Globalization;
using System.Text;
using Olbrasoft.Text.Similarity;

namespace Olbrasoft.VoiceAssistant.ContinuousListener.Services;

/// <summary>
/// Tracks what the assistant is currently saying (TTS output).
/// Used to filter out assistant's own speech from transcriptions.
/// Now supports multiple TTS messages that arrive in sequence.
/// </summary>
public class AssistantSpeechTrackerService
{
    private readonly ILogger<AssistantSpeechTrackerService> _logger;
    private readonly IStringSimilarity _stringSimilarity;
    private readonly object _lock = new();
    
    // History of TTS messages (multiple can arrive before Whisper returns)
    private readonly List<string> _ttsHistory = new();
    private const int MaxHistorySize = 10;
    
    // Similarity threshold for fuzzy matching (0.0 - 1.0)
    private const double SimilarityThreshold = 0.75;

    public AssistantSpeechTrackerService(ILogger<AssistantSpeechTrackerService> logger, IStringSimilarity stringSimilarity)
    {
        _logger = logger;
        _stringSimilarity = stringSimilarity;
    }

    /// <summary>
    /// Called when assistant starts speaking. Adds to history instead of replacing.
    /// </summary>
    public void StartSpeaking(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;
            
        lock (_lock)
        {
            _ttsHistory.Add(text);
            
            // Limit history size
            while (_ttsHistory.Count > MaxHistorySize)
            {
                _ttsHistory.RemoveAt(0);
            }
            
            Console.WriteLine($"\u001b[95m📢 TTS History [{_ttsHistory.Count}]: \"{(text.Length > 60 ? text[..60] + "..." : text)}\"\u001b[0m");
        }
    }

    /// <summary>
    /// Called when assistant stops speaking.
    /// </summary>
    public void StopSpeaking()
    {
        // No-op - history is cleared explicitly after transcription processing
    }

    /// <summary>
    /// Filters out all TTS echo messages from the transcription.
    /// Iterates through TTS history and removes matching prefixes.
    /// </summary>
    /// <param name="transcription">The full transcription from Whisper</param>
    /// <returns>Cleaned text with TTS echoes removed</returns>
    public string FilterEchoFromTranscription(string transcription)
    {
        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(transcription))
                return transcription;
                
            if (_ttsHistory.Count == 0)
                return transcription;
            
            var result = transcription;
            var removedCount = 0;
            
            Console.WriteLine($"\u001b[96m🔍 Filtering echo from: \"{(result.Length > 80 ? result[..80] + "..." : result)}\"\u001b[0m");
            Console.WriteLine($"\u001b[96m   TTS History has {_ttsHistory.Count} message(s)\u001b[0m");
            
            // Iterate through TTS history and try to remove each from the beginning
            foreach (var ttsMessage in _ttsHistory)
            {
                var (wasRemoved, newResult, similarity) = TryRemovePrefix(result, ttsMessage);
                
                if (wasRemoved)
                {
                    removedCount++;
                    Console.WriteLine($"\u001b[93m   ✂️ Removed echo (similarity: {similarity:P0}): \"{(ttsMessage.Length > 50 ? ttsMessage[..50] + "..." : ttsMessage)}\"\u001b[0m");
                    result = newResult;
                    
                    // If nothing left, we're done
                    if (string.IsNullOrWhiteSpace(result))
                        break;
                }
            }
            
            // DON'T clear history here - it will be cleared before sending to LLM hub
            
            if (removedCount > 0)
            {
                Console.WriteLine($"\u001b[92m   ✅ Filtered {removedCount} echo(es). Result: \"{(result.Length > 80 ? result[..80] + "..." : result)}\"\u001b[0m");
            }
            else
            {
                Console.WriteLine($"\u001b[91m   ❌ No echo detected! TTS history:\u001b[0m");
                foreach (var tts in _ttsHistory)
                {
                    Console.WriteLine($"\u001b[91m      - \"{(tts.Length > 60 ? tts[..60] + "..." : tts)}\"\u001b[0m");
                }
            }
            
            return result.Trim();
        }
    }

    /// <summary>
    /// Tries to remove a TTS message from the beginning of the text using fuzzy matching.
    /// </summary>
    private (bool wasRemoved, string newText, double similarity) TryRemovePrefix(string text, string ttsMessage)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(ttsMessage))
            return (false, text, 0.0);
            
        var textNormalized = NormalizeText(text);
        var ttsNormalized = NormalizeText(ttsMessage);
        
        var textWords = textNormalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var ttsWords = ttsNormalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (textWords.Length == 0 || ttsWords.Length == 0)
            return (false, text, 0.0);
            
        // Take prefix from text with same word count as TTS (+/- some tolerance)
        // We allow some tolerance because Whisper might add/remove words
        var minPrefixLen = Math.Max(1, ttsWords.Length - 2);
        var maxPrefixLen = Math.Min(textWords.Length, ttsWords.Length + 2);
        
        double bestSimilarity = 0;
        int bestPrefixLength = 0;
        
        // Try different prefix lengths and find best match
        for (int prefixLen = minPrefixLen; prefixLen <= maxPrefixLen; prefixLen++)
        {
            var prefix = string.Join(" ", textWords.Take(prefixLen));
            var similarity = CalculateSimilarity(prefix, ttsNormalized);
            
            Console.WriteLine($"\u001b[90m      [DEBUG] prefixLen={prefixLen}, similarity={similarity:P1}, prefix=\"{prefix}\"\u001b[0m");
            
            if (similarity > bestSimilarity)
            {
                bestSimilarity = similarity;
                bestPrefixLength = prefixLen;
            }
        }
        
        Console.WriteLine($"\u001b[96m      [DEBUG] Best: prefixLen={bestPrefixLength}, similarity={bestSimilarity:P1}, threshold={SimilarityThreshold:P0}\u001b[0m");
        Console.WriteLine($"\u001b[96m      [DEBUG] TTS normalized: \"{ttsNormalized}\"\u001b[0m");
        
        if (bestSimilarity >= SimilarityThreshold)
        {
            // Remove the prefix from original text
            // We need to find where to cut in the ORIGINAL text (not normalized)
            var remainingText = RemoveWordsFromOriginal(text, bestPrefixLength);
            return (true, remainingText, bestSimilarity);
        }
        
        return (false, text, bestSimilarity);
    }

    /// <summary>
    /// Removes N words from the beginning of the original text (preserving original formatting).
    /// </summary>
    private static string RemoveWordsFromOriginal(string text, int wordCount)
    {
        if (string.IsNullOrWhiteSpace(text) || wordCount <= 0)
            return text;
            
        // Split by whitespace but keep track of positions
        var words = new List<(int start, int end)>();
        int i = 0;
        
        while (i < text.Length)
        {
            // Skip whitespace
            while (i < text.Length && char.IsWhiteSpace(text[i]))
                i++;
                
            if (i >= text.Length)
                break;
                
            int wordStart = i;
            
            // Find end of word
            while (i < text.Length && !char.IsWhiteSpace(text[i]))
                i++;
                
            words.Add((wordStart, i));
        }
        
        if (wordCount >= words.Count)
            return string.Empty;
            
        // Return text starting after the Nth word
        int cutPosition = words[wordCount].start;
        return text[cutPosition..].TrimStart();
    }

    /// <summary>
    /// Normalizes text for comparison (lowercase, remove punctuation, trim).
    /// </summary>
    private static string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Convert to lowercase and remove common punctuation
        var normalized = text.ToLowerInvariant()
            .Replace(".", "")
            .Replace(",", "")
            .Replace("!", "")
            .Replace("?", "")
            .Replace(":", "")
            .Replace(";", "")
            .Replace("\"", "")
            .Replace("'", "")
            .Replace("\u201E", "") // Czech opening quote „
            .Replace("\u201C", "") // Czech closing quote "
            .Replace("(", "")
            .Replace(")", "")
            .Replace("[", "")
            .Replace("]", "")
            .Trim();

        // Normalize whitespace
        while (normalized.Contains("  "))
        {
            normalized = normalized.Replace("  ", " ");
        }

        return normalized;
    }

    /// <summary>
    /// Calculates similarity between two strings using the injected similarity algorithm.
    /// Returns value between 0.0 (no match) and 1.0 (perfect match).
    /// </summary>
    private double CalculateSimilarity(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            return 0.0;

        // Normalize both strings (remove diacritics for Czech language support)
        var normalizedA = RemoveDiacritics(a.ToLowerInvariant());
        var normalizedB = RemoveDiacritics(b.ToLowerInvariant());

        return _stringSimilarity.Similarity(normalizedA, normalizedB);
    }

    /// <summary>
    /// Removes diacritics from text (e.g., "úspěšně" → "uspesne").
    /// Essential for comparing Czech text with potential ASR transcription errors.
    /// </summary>
    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        
        foreach (char c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Gets the TTS history count (for debugging).
    /// </summary>
    public int GetHistoryCount()
    {
        lock (_lock)
        {
            return _ttsHistory.Count;
        }
    }

    /// <summary>
    /// Clears the TTS history.
    /// </summary>
    public void ClearHistory()
    {
        lock (_lock)
        {
            _ttsHistory.Clear();
        }
    }

    /// <summary>
    /// Checks if any TTS message in history contains one of the stop words.
    /// Used to distinguish between user's "stop" command and TTS echo.
    /// </summary>
    /// <param name="stopWords">Collection of stop words to check for</param>
    /// <returns>True if any TTS message contains a stop word</returns>
    public bool ContainsStopWord(IEnumerable<string> stopWords)
    {
        lock (_lock)
        {
            if (_ttsHistory.Count == 0)
                return false;
                
            foreach (var ttsMessage in _ttsHistory)
            {
                var normalized = NormalizeText(ttsMessage);
                foreach (var stopWord in stopWords)
                {
                    var normalizedStopWord = stopWord.ToLowerInvariant();
                    // Check if the stop word is present as a whole word
                    if (normalized.Contains(normalizedStopWord))
                    {
                        _logger.LogDebug("Found stop word '{StopWord}' in TTS history: {Message}", 
                            stopWord, ttsMessage);
                        return true;
                    }
                }
            }
            return false;
        }
    }

    // ========== LEGACY METHODS (kept for compatibility) ==========

    /// <summary>
    /// Checks if a transcription matches the assistant's recent speech.
    /// </summary>
    [Obsolete("Use FilterEchoFromTranscription() instead")]
    public bool IsAssistantSpeech(string transcription)
    {
        var filtered = FilterEchoFromTranscription(transcription);
        return string.IsNullOrWhiteSpace(filtered);
    }

    /// <summary>
    /// Gets the current/recent speech text (for debugging).
    /// </summary>
    [Obsolete("Use GetHistoryCount() instead")]
    public string? GetCurrentSpeechText()
    {
        lock (_lock)
        {
            return _ttsHistory.Count > 0 ? _ttsHistory[^1] : null;
        }
    }

    /// <summary>
    /// Legacy method - now uses FilterEchoFromTranscription internally.
    /// </summary>
    [Obsolete("Use FilterEchoFromTranscription() instead")]
    public (bool isEcho, double similarity, string remainingText) DetectEchoAndExtractRemaining(string transcription)
    {
        var filtered = FilterEchoFromTranscription(transcription);
        var isFullEcho = string.IsNullOrWhiteSpace(filtered);
        return (isFullEcho, isFullEcho ? 1.0 : 0.0, filtered);
    }
}
