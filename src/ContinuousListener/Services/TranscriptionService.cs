using Microsoft.Extensions.Logging;
using Olbrasoft.VoiceAssistant.Shared.Speech;

namespace Olbrasoft.VoiceAssistant.ContinuousListener.Services;

/// <summary>
/// Service for transcribing audio using Whisper.net.
/// Supports two models: fast (for wake word detection) and accurate (for full transcription).
/// </summary>
public class TranscriptionService : IDisposable
{
    private readonly ILogger<TranscriptionService> _logger;
    private readonly ContinuousListenerOptions _options;
    private WhisperNetTranscriber? _fastTranscriber;
    private WhisperNetTranscriber? _accurateTranscriber;
    private bool _disposed;

    public TranscriptionService(ILogger<TranscriptionService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _options = new ContinuousListenerOptions();
        configuration.GetSection(ContinuousListenerOptions.SectionName).Bind(_options);
    }

    /// <summary>
    /// Initializes both Whisper transcibers (fast and accurate).
    /// </summary>
    public void Initialize()
    {
        var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // Initialize fast model for wake word detection
        if (_fastTranscriber == null && !string.IsNullOrEmpty(_options.WhisperFastModelPath))
        {
            _logger.LogInformation("Loading FAST Whisper model from: {Path}", _options.WhisperFastModelPath);
            var fastLogger = loggerFactory.CreateLogger<WhisperNetTranscriber>();
            _fastTranscriber = new WhisperNetTranscriber(fastLogger, _options.WhisperFastModelPath, _options.WhisperLanguage);
            _logger.LogInformation("✅ Fast Whisper model loaded (for wake word detection)");
        }

        // Initialize accurate model for full transcription
        if (_accurateTranscriber == null)
        {
            _logger.LogInformation("Loading ACCURATE Whisper model from: {Path}", _options.WhisperModelPath);
            var accurateLogger = loggerFactory.CreateLogger<WhisperNetTranscriber>();
            _accurateTranscriber = new WhisperNetTranscriber(accurateLogger, _options.WhisperModelPath, _options.WhisperLanguage);
            _logger.LogInformation("✅ Accurate Whisper model loaded (for full transcription)");
        }
    }

    /// <summary>
    /// Transcribes audio data using the FAST model (for wake word detection).
    /// </summary>
    /// <param name="audioData">16-bit PCM audio data at 16kHz.</param>
    /// <returns>Transcription result.</returns>
    public async Task<TranscriptionResult> TranscribeFastAsync(byte[] audioData)
    {
        if (_fastTranscriber == null)
        {
            // Fallback to accurate if fast not available
            return await TranscribeAsync(audioData);
        }

        var result = await _fastTranscriber.TranscribeAsync(audioData);
        return result;
    }

    /// <summary>
    /// Transcribes audio data using the ACCURATE model (for full command transcription).
    /// </summary>
    /// <param name="audioData">16-bit PCM audio data at 16kHz.</param>
    /// <returns>Transcription result.</returns>
    public async Task<TranscriptionResult> TranscribeAsync(byte[] audioData)
    {
        if (_accurateTranscriber == null)
        {
            throw new InvalidOperationException("Transcriber not initialized. Call Initialize() first.");
        }

        var result = await _accurateTranscriber.TranscribeAsync(audioData);
        return result;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _fastTranscriber?.Dispose();
        _fastTranscriber = null;
        _accurateTranscriber?.Dispose();
        _accurateTranscriber = null;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
