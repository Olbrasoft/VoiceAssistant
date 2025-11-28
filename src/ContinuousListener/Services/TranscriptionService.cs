using Microsoft.Extensions.Logging;
using Olbrasoft.VoiceAssistant.Shared.Speech;

namespace Olbrasoft.VoiceAssistant.ContinuousListener.Services;

/// <summary>
/// Service for transcribing audio using Whisper.net.
/// </summary>
public class TranscriptionService : IDisposable
{
    private readonly ILogger<TranscriptionService> _logger;
    private readonly ContinuousListenerOptions _options;
    private WhisperNetTranscriber? _transcriber;
    private bool _disposed;

    public TranscriptionService(ILogger<TranscriptionService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _options = new ContinuousListenerOptions();
        configuration.GetSection(ContinuousListenerOptions.SectionName).Bind(_options);
    }

    /// <summary>
    /// Initializes the Whisper transcriber.
    /// </summary>
    public void Initialize()
    {
        if (_transcriber != null)
        {
            _logger.LogWarning("Transcriber already initialized");
            return;
        }

        _logger.LogInformation("Loading Whisper model from: {Path}", _options.WhisperModelPath);
        
        var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        var whisperLogger = loggerFactory.CreateLogger<WhisperNetTranscriber>();
        
        _transcriber = new WhisperNetTranscriber(whisperLogger, _options.WhisperModelPath, _options.WhisperLanguage);
        
        _logger.LogInformation("Whisper model loaded successfully (language: {Language})", _options.WhisperLanguage);
    }

    /// <summary>
    /// Transcribes audio data to text.
    /// </summary>
    /// <param name="audioData">16-bit PCM audio data at 16kHz.</param>
    /// <returns>Transcription result.</returns>
    public async Task<TranscriptionResult> TranscribeAsync(byte[] audioData)
    {
        if (_transcriber == null)
        {
            throw new InvalidOperationException("Transcriber not initialized. Call Initialize() first.");
        }

        var result = await _transcriber.TranscribeAsync(audioData);
        return result;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _transcriber?.Dispose();
        _transcriber = null;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
