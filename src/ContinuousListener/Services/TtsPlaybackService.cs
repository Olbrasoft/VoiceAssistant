using System.Text;
using System.Text.Json;

namespace Olbrasoft.VoiceAssistant.ContinuousListener.Services;

/// <summary>
/// Service for playing TTS responses via Edge TTS WebSocket Server API.
/// Used for speaking Groq router responses to the user.
/// </summary>
public class TtsPlaybackService
{
    private readonly ILogger<TtsPlaybackService> _logger;
    private readonly HttpClient _httpClient;
    private readonly AssistantSpeechTrackerService _speechTracker;
    private readonly string _ttsApiUrl;
    private const string SpeechLockFile = "/tmp/speech-lock";

    public TtsPlaybackService(
        ILogger<TtsPlaybackService> logger, 
        IConfiguration configuration,
        AssistantSpeechTrackerService speechTracker)
    {
        _logger = logger;
        _speechTracker = speechTracker;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _ttsApiUrl = configuration.GetValue<string>("TtsApiUrl") ?? "http://localhost:5555";
    }

    /// <summary>
    /// Speaks the given text using TTS.
    /// Respects speech lock (when user is recording).
    /// </summary>
    /// <param name="text">Text to speak.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if speech was initiated successfully.</returns>
    public async Task<bool> SpeakAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogDebug("Empty text provided, skipping TTS");
            return false;
        }

        // Check speech lock (user is recording)
        if (File.Exists(SpeechLockFile))
        {
            // Silent skip - no log output to terminal
            return false;
        }

        try
        {
            var requestBody = new
            {
                text = text,
                play = true
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("🔊 Speaking: {Text}", 
                text.Length > 100 ? text[..100] + "..." : text);

            // Track what we're saying for echo detection
            _speechTracker.StartSpeaking(text);

            var response = await _httpClient.PostAsync(
                $"{_ttsApiUrl}/api/speech/speak", 
                content, 
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("TTS request accepted");
                // Note: StopSpeaking will be called by cooldown timeout in AssistantSpeechTrackerService
                // The TTS server plays audio asynchronously, so we can't know exactly when it finishes
                return true;
            }
            else
            {
                _speechTracker.StopSpeaking();
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("TTS request failed: {StatusCode} - {Error}", 
                    response.StatusCode, errorBody);
                return false;
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("TTS request cancelled");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling TTS API");
            return false;
        }
    }

    /// <summary>
    /// Stops any currently playing TTS audio.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if stop was successful.</returns>
    public async Task<bool> StopAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"{_ttsApiUrl}/api/speech/stop", 
                null, 
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("TTS stopped");
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to stop TTS: {StatusCode}", response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not stop TTS");
            return false;
        }
    }

    /// <summary>
    /// Checks if the TTS server is available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if TTS server is responding.</returns>
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(_ttsApiUrl, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
