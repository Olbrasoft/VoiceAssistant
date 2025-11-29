namespace Olbrasoft.VoiceAssistant.ContinuousListener.Services;

/// <summary>
/// Service for controlling the TTS (Text-to-Speech) server.
/// </summary>
public class TtsControlService
{
    private readonly ILogger<TtsControlService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _ttsApiUrl;

    public TtsControlService(ILogger<TtsControlService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        _ttsApiUrl = configuration.GetValue<string>("TtsApiUrl") ?? "http://localhost:5555";
    }

    /// <summary>
    /// Stops any currently playing TTS audio.
    /// </summary>
    /// <returns>True if stop was successful or nothing was playing.</returns>
    public async Task<bool> StopAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync($"{_ttsApiUrl}/api/speech/stop", null, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("🔇 TTS stopped");
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
            _logger.LogDebug(ex, "Could not stop TTS (server may not be running)");
            return false;
        }
    }
}
