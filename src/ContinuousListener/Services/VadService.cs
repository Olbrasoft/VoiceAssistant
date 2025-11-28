using Microsoft.Extensions.Logging;

namespace Olbrasoft.VoiceAssistant.ContinuousListener.Services;

/// <summary>
/// Voice Activity Detection service using RMS-based thresholding.
/// </summary>
public class VadService
{
    private readonly ILogger<VadService> _logger;
    private readonly ContinuousListenerOptions _options;

    public VadService(ILogger<VadService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _options = new ContinuousListenerOptions();
        configuration.GetSection(ContinuousListenerOptions.SectionName).Bind(_options);
    }

    /// <summary>
    /// Calculates RMS (Root Mean Square) of audio data.
    /// </summary>
    /// <param name="pcmData">16-bit PCM audio data.</param>
    /// <returns>RMS value normalized to 0.0 - 1.0 range.</returns>
    public float CalculateRms(byte[] pcmData)
    {
        int sampleCount = pcmData.Length / 2;
        if (sampleCount == 0) return 0;

        double sumSquares = 0;

        for (int i = 0; i < sampleCount; i++)
        {
            short sample = BitConverter.ToInt16(pcmData, i * 2);
            float normalized = sample / 32768.0f;
            sumSquares += normalized * normalized;
        }

        return (float)Math.Sqrt(sumSquares / sampleCount);
    }

    /// <summary>
    /// Detects if the audio chunk contains speech.
    /// </summary>
    /// <param name="pcmData">16-bit PCM audio data.</param>
    /// <returns>True if speech detected, false otherwise.</returns>
    public bool IsSpeech(byte[] pcmData)
    {
        float rms = CalculateRms(pcmData);
        return rms > _options.SilenceThreshold;
    }

    /// <summary>
    /// Detects if the audio chunk contains speech and returns the RMS value.
    /// </summary>
    /// <param name="pcmData">16-bit PCM audio data.</param>
    /// <returns>Tuple of (isSpeech, rmsValue).</returns>
    public (bool IsSpeech, float Rms) Analyze(byte[] pcmData)
    {
        float rms = CalculateRms(pcmData);
        return (rms > _options.SilenceThreshold, rms);
    }

    /// <summary>
    /// Gets the current silence threshold.
    /// </summary>
    public float SilenceThreshold => _options.SilenceThreshold;
}
