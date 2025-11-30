using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Olbrasoft.VoiceAssistant.ContinuousListener.Services;

/// <summary>
/// Base class for OpenAI-compatible LLM router services.
/// Contains shared logic for system prompt, parsing, and context management.
/// </summary>
public abstract class BaseLlmRouterService : ILlmRouterService
{
    protected readonly ILogger _logger;
    protected readonly HttpClient _httpClient;
    protected readonly string _model;

    // Recent context for multi-turn awareness
    private readonly Queue<ContextEntry> _recentContext = new();
    private const int MaxContextEntries = 5;

    public abstract string ProviderName { get; }

    protected BaseLlmRouterService(ILogger logger, HttpClient httpClient, string model)
    {
        _logger = logger;
        _httpClient = httpClient;
        _model = model;
    }

    public async Task<LlmRouterResult> RouteAsync(string inputText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(inputText))
        {
            return LlmRouterResult.Ignored("Empty input");
        }

        var stopwatch = Stopwatch.StartNew();

        var systemPrompt = BuildSystemPrompt();
        var userMessage = $"Voice assistant zachytil: \"{inputText}\"";

        var request = new LlmRequest
        {
            Model = _model,
            Messages = new[]
            {
                new LlmMessage { Role = "system", Content = systemPrompt },
                new LlmMessage { Role = "user", Content = userMessage }
            },
            Temperature = 0.2f,
            MaxTokens = 256
        };

        // Retry logic for rate limiting
        const int maxRetries = 3;
        var retryDelays = new[] { 1000, 2000, 4000 }; // ms

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogDebug("Sending to {Provider}: {Input} (attempt {Attempt})", ProviderName, inputText, attempt + 1);

                var response = await _httpClient.PostAsJsonAsync("chat/completions", request, cancellationToken);

                // Handle rate limiting with retry
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("{Provider} rate limit response: {Body}", ProviderName, errorBody);

                    if (attempt < maxRetries)
                    {
                        var delay = retryDelays[attempt];
                        _logger.LogWarning("Rate limited (429), waiting {Delay}ms before retry {Attempt}/{Max}",
                            delay, attempt + 2, maxRetries + 1);
                        await Task.Delay(delay, cancellationToken);
                        continue;
                    }
                    else
                    {
                        _logger.LogError("Rate limited (429) after {MaxRetries} retries", maxRetries + 1);
                        return LlmRouterResult.Error("Rate limited - try again later", (int)stopwatch.ElapsedMilliseconds);
                    }
                }

                response.EnsureSuccessStatusCode();

                var llmResponse = await response.Content.ReadFromJsonAsync<LlmResponse>(cancellationToken: cancellationToken);
                stopwatch.Stop();

                var content = llmResponse?.Choices?.FirstOrDefault()?.Message?.Content;
                if (string.IsNullOrEmpty(content))
                {
                    _logger.LogWarning("Empty response from {Provider}", ProviderName);
                    return LlmRouterResult.Error("Empty response", (int)stopwatch.ElapsedMilliseconds);
                }

                // Parse JSON response
                var result = ParseLlmResponse(content, (int)stopwatch.ElapsedMilliseconds);

                // Add to context
                AddToContext(inputText, result);

                _logger.LogInformation(
                    "{Provider} routing: {Action} (confidence: {Confidence:F2}, time: {Time}ms)",
                    ProviderName, result.Action, result.Confidence, result.ResponseTimeMs);

                return result;
            }
            catch (HttpRequestException ex)
            {
                if (attempt < maxRetries)
                {
                    var delay = retryDelays[attempt];
                    _logger.LogWarning(ex, "HTTP error, retrying in {Delay}ms (attempt {Attempt}/{Max})",
                        delay, attempt + 2, maxRetries + 1);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }
                stopwatch.Stop();
                _logger.LogError(ex, "HTTP error calling {Provider} API after {MaxRetries} retries", ProviderName, maxRetries + 1);
                return LlmRouterResult.Error($"HTTP error: {ex.Message}", (int)stopwatch.ElapsedMilliseconds);
            }
            catch (TaskCanceledException ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(ex, "{Provider} API call timed out", ProviderName);
                return LlmRouterResult.Error("Timeout", (int)stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Unexpected error calling {Provider} API", ProviderName);
                return LlmRouterResult.Error(ex.Message, (int)stopwatch.ElapsedMilliseconds);
            }
        }

        stopwatch.Stop();
        return LlmRouterResult.Error("Max retries exceeded", (int)stopwatch.ElapsedMilliseconds);
    }

    private string BuildSystemPrompt()
    {
        var now = DateTime.Now;
        var dayOfWeek = now.DayOfWeek switch
        {
            DayOfWeek.Monday => "pondělí",
            DayOfWeek.Tuesday => "úterý",
            DayOfWeek.Wednesday => "středa",
            DayOfWeek.Thursday => "čtvrtek",
            DayOfWeek.Friday => "pátek",
            DayOfWeek.Saturday => "sobota",
            DayOfWeek.Sunday => "neděle",
            _ => now.DayOfWeek.ToString()
        };

        var contextSection = "";
        if (_recentContext.Count > 0)
        {
            var contextLines = _recentContext
                .Select(c => $"- [{c.Timestamp:HH:mm:ss}] \"{c.Input}\" → {c.Action}")
                .ToList();
            contextSection = $@"

PŘEDCHOZÍ KONTEXT (posledních {_recentContext.Count} interakcí):
{string.Join("\n", contextLines)}";
        }

        return $@"Jsi Voice Router - součást voice assistenta běžícího na Linuxovém desktopu.

KONTEXT:
- Uživatel má spuštěný program OpenCode (AI coding agent v terminálu)
- Voice assistant průběžně zachytává hlasový vstup
- Uživatel může říct ""počítači"" nebo ""open code"" jako wake word
- Aktuální čas: {now:HH:mm}
- Aktuální datum: {now:d.M.yyyy} ({dayOfWeek}){contextSection}

TVŮJ ÚKOL:
Analyzuj zachycený text a rozhodni, jak s ním naložit:

1. **ROUTE to OpenCode** (action: ""opencode"") - PREFEROVANÁ VOLBA
   - Cokoliv co obsahuje ""počítači"" nebo ""open code"" - VŽDY do OpenCode!
   - Příkazy pro programování, práci s kódem, soubory, terminálem
   - Technické dotazy vyžadující kontext projektu
   - Příkazy jako: ""vytvoř"", ""oprav"", ""najdi"", ""spusť testy"", ""commitni""
   - Jakékoliv komplexní požadavky nebo dotazy
   - Když si nejsi jistý - pošli do OpenCode!
   - Příkazy jako ""podívej se"", ""zkontroluj"", ""zjisti"" - do OpenCode!
   - Otevírání aplikací: ""otevři VS Code"", ""spusť prohlížeč"" - TAKÉ do OpenCode!
   - Spouštění příkazů, bash, terminál - VŽDY do OpenCode!

2. **RESPOND directly** (action: ""respond"") 
   - POUZE jednoduché faktické dotazy bez potřeby kontextu
   - Čas, datum, den v týdnu
   - Jednoduché výpočty (2+2)
   - Vrať odpověď v ""response"" poli - KRÁTCE, pro TTS přehrání (1-2 věty)

3. **IGNORE** (action: ""ignore"")
   - Náhodná konverzace s někým jiným (bez wake word)
   - Neúplné věty, šum
   - Text bez jasného záměru a bez wake word

ODPOVĚZ POUZE TÍMTO JSON (žádný další text):
{{
    ""action"": ""opencode"" | ""respond"" | ""ignore"",
    ""is_question"": true | false,
    ""confidence"": 0.0-1.0,
    ""reason"": ""krátké zdůvodnění"",
    ""response"": ""odpověď pro TTS (pokud action=respond, jinak null)"",
    ""command_for_opencode"": ""shrnutí příkazu (pouze pokud action=opencode, jinak null)""
}}

POLE is_question:
- true = otázka/dotaz (""jak"", ""co"", ""proč"", ""kde"", ""který"", ""jaký"", požadavek na vysvětlení)
- false = příkaz/instrukce (""vytvoř"", ""oprav"", ""spusť"", ""commitni"", ""otevři"", imperativ)";
    }

    private LlmRouterResult ParseLlmResponse(string content, int responseTimeMs)
    {
        try
        {
            // Clean up content - remove markdown code blocks if present
            var json = content.Trim();
            if (json.StartsWith("```"))
            {
                var lines = json.Split('\n');
                json = string.Join("\n", lines.Skip(1).TakeWhile(l => !l.StartsWith("```")));
            }

            var parsed = JsonSerializer.Deserialize<LlmRouterResponseDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed == null)
            {
                return LlmRouterResult.Error("Failed to parse JSON", responseTimeMs);
            }

            var action = parsed.Action?.ToLowerInvariant() switch
            {
                "opencode" => LlmRouterAction.OpenCode,
                "respond" => LlmRouterAction.Respond,
                "ignore" => LlmRouterAction.Ignore,
                // Bash actions are now redirected to OpenCode
                "bash" => LlmRouterAction.OpenCode,
                _ => LlmRouterAction.Ignore
            };

            return new LlmRouterResult
            {
                Action = action,
                IsQuestion = parsed.IsQuestion,
                Confidence = parsed.Confidence,
                Reason = parsed.Reason,
                Response = parsed.Response,
                CommandForOpenCode = parsed.CommandForOpenCode,
                BashCommand = parsed.BashCommand,
                ResponseTimeMs = responseTimeMs,
                Success = true
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse {Provider} response: {Content}", ProviderName, content);
            return LlmRouterResult.Error($"JSON parse error: {ex.Message}", responseTimeMs);
        }
    }

    private void AddToContext(string input, LlmRouterResult result)
    {
        _recentContext.Enqueue(new ContextEntry
        {
            Input = input.Length > 100 ? input[..100] + "..." : input,
            Action = result.Action.ToString(),
            Timestamp = DateTime.Now
        });

        while (_recentContext.Count > MaxContextEntries)
        {
            _recentContext.Dequeue();
        }
    }

    private record ContextEntry
    {
        public required string Input { get; init; }
        public required string Action { get; init; }
        public DateTime Timestamp { get; init; }
    }

    #region DTOs

    private class LlmRequest
    {
        [JsonPropertyName("model")]
        public required string Model { get; set; }

        [JsonPropertyName("messages")]
        public required LlmMessage[] Messages { get; set; }

        [JsonPropertyName("temperature")]
        public float Temperature { get; set; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }
    }

    private class LlmMessage
    {
        [JsonPropertyName("role")]
        public required string Role { get; set; }

        [JsonPropertyName("content")]
        public required string Content { get; set; }
    }

    private class LlmResponse
    {
        [JsonPropertyName("choices")]
        public LlmChoice[]? Choices { get; set; }
    }

    private class LlmChoice
    {
        [JsonPropertyName("message")]
        public LlmMessage? Message { get; set; }
    }

    private class LlmRouterResponseDto
    {
        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("is_question")]
        public bool IsQuestion { get; set; }

        [JsonPropertyName("confidence")]
        public float Confidence { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        [JsonPropertyName("response")]
        public string? Response { get; set; }

        [JsonPropertyName("command_for_opencode")]
        public string? CommandForOpenCode { get; set; }

        [JsonPropertyName("bash_command")]
        public string? BashCommand { get; set; }
    }

    #endregion
}
