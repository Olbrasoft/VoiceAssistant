using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Olbrasoft.VoiceAssistant.Shared.Speech;

/// <summary>
/// Decodes Whisper token IDs to text.
/// </summary>
public class TokenDecoder
{
    private readonly Dictionary<int, string> _tokenMap;
    private readonly Dictionary<string, int> _reverseTokenMap;
    
    // Special tokens (from Whisper tokenizer)
    private const int EndOfTextToken = 50257;
    private const int StartOfTranscriptToken = 50258;
    private const int StartOfPrevToken = 50361;  // <|startofprev|>
    private const int TranslateToken = 50358;
    private const int TranscribeToken = 50359;
    private const int NoTimestampsToken = 50363;
    
    public TokenDecoder(string tokensFilePath)
    {
        _tokenMap = LoadTokens(tokensFilePath);
        // Build reverse map - use first occurrence of each token string
        _reverseTokenMap = new Dictionary<string, int>();
        foreach (var kvp in _tokenMap)
        {
            _reverseTokenMap.TryAdd(kvp.Value, kvp.Key);
        }
    }
    
    /// <summary>
    /// Loads tokens from file (format: base64_token token_id)
    /// </summary>
    private Dictionary<int, string> LoadTokens(string filePath)
    {
        var map = new Dictionary<int, string>();
        
        foreach (var line in File.ReadAllLines(filePath))
        {
            var parts = line.Split(' ', 2);
            if (parts.Length == 2 && int.TryParse(parts[1], out int tokenId))
            {
                try
                {
                    // Decode base64 to get the actual token string
                    byte[] data = Convert.FromBase64String(parts[0]);
                    string token = Encoding.UTF8.GetString(data);
                    map[tokenId] = token;
                }
                catch
                {
                    // Skip invalid tokens
                }
            }
        }
        
        return map;
    }
    
    /// <summary>
    /// Decodes a sequence of token IDs to text.
    /// </summary>
    public string Decode(int[] tokenIds)
    {
        var sb = new StringBuilder();
        
        foreach (int tokenId in tokenIds)
        {
            // Stop at end of text token
            if (tokenId == EndOfTextToken)
                break;
                
            // Skip special tokens
            if (IsSpecialToken(tokenId))
                continue;
            
            if (_tokenMap.TryGetValue(tokenId, out string? token))
            {
                sb.Append(token);
            }
        }
        
        return sb.ToString().Trim();
    }
    
    private static bool IsSpecialToken(int tokenId)
    {
        return tokenId >= StartOfTranscriptToken;
    }
    
    /// <summary>
    /// Encodes text to token IDs using greedy longest-match tokenization.
    /// This is a simplified version - full BPE would be more accurate.
    /// </summary>
    public int[] Encode(string text)
    {
        if (string.IsNullOrEmpty(text))
            return Array.Empty<int>();
        
        var tokens = new List<int>();
        int i = 0;
        
        while (i < text.Length)
        {
            // Try to find longest matching token
            int bestLength = 0;
            int bestToken = -1;
            
            // Check substrings of decreasing length
            for (int len = Math.Min(20, text.Length - i); len >= 1; len--)
            {
                string substr = text.Substring(i, len);
                if (_reverseTokenMap.TryGetValue(substr, out int tokenId))
                {
                    bestLength = len;
                    bestToken = tokenId;
                    break;
                }
            }
            
            if (bestToken >= 0)
            {
                tokens.Add(bestToken);
                i += bestLength;
            }
            else
            {
                // Single character fallback - try with space prefix (Whisper uses Ġ for space)
                // Skip character if no match found
                i++;
            }
        }
        
        return tokens.ToArray();
    }
    
    /// <summary>
    /// Gets the start tokens for Czech transcription (no timestamps).
    /// </summary>
    public static int[] GetStartTokens(string language = "cs")
    {
        // Whisper start sequence: <|startoftranscript|><|cs|><|transcribe|><|notimestamps|>
        int languageToken = GetLanguageToken(language);
        
        return new[]
        {
            StartOfTranscriptToken,  // 50258
            languageToken,            // Language specific
            TranscribeToken,          // 50359
            NoTimestampsToken         // 50363
        };
    }
    
    /// <summary>
    /// Gets the start tokens with previous context (for condition_on_previous_text).
    /// Format: <|startofprev|> [previous tokens] <|startoftranscript|><|cs|><|transcribe|><|notimestamps|>
    /// </summary>
    public int[] GetStartTokensWithContext(string previousText, string language = "cs")
    {
        var tokens = new List<int>();
        
        if (!string.IsNullOrWhiteSpace(previousText))
        {
            // Add previous context marker and tokenized previous text
            tokens.Add(StartOfPrevToken);  // <|startofprev|>
            tokens.AddRange(Encode(previousText));
        }
        
        // Add standard start tokens
        tokens.AddRange(GetStartTokens(language));
        
        return tokens.ToArray();
    }
    
    private static int GetLanguageToken(string language)
    {
        // Language tokens start at 50259
        // Common languages (this is simplified - full list has ~100 languages)
        return language.ToLower() switch
        {
            "en" => 50259,
            "zh" => 50260,
            "de" => 50261,
            "es" => 50262,
            "ru" => 50263,
            "ko" => 50264,
            "fr" => 50265,
            "ja" => 50266,
            "pt" => 50267,
            "tr" => 50268,
            "pl" => 50269,
            "ca" => 50270,
            "nl" => 50271,
            "ar" => 50272,
            "sv" => 50273,
            "it" => 50274,
            "id" => 50275,
            "hi" => 50276,
            "fi" => 50277,
            "vi" => 50278,
            "he" => 50279,
            "uk" => 50280,
            "el" => 50281,
            "ms" => 50282,
            "cs" => 50283,  // Czech
            "ro" => 50284,
            "da" => 50285,
            "hu" => 50286,
            "ta" => 50287,
            _ => 50259 // Default to English
        };
    }
}
