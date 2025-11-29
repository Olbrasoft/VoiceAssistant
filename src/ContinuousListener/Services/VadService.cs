using Microsoft.Extensions.Logging;

namespace Olbrasoft.VoiceAssistant.ContinuousListener.Services;

/// <summary>
/// Voice Activity Detection service using RMS-based thresholding.
/// Supports automatic calibration to ambient noise level.
/// </summary>
public class VadService
{
    private readonly ILogger<VadService> _logger;
    private readonly ContinuousListenerOptions _options;
    
    // Dynamic threshold after calibration
    private float _dynamicThreshold;
    private bool _isCalibrated;

    public VadService(ILogger<VadService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _options = new ContinuousListenerOptions();
        configuration.GetSection(ContinuousListenerOptions.SectionName).Bind(_options);
        
        // Start with configured threshold
        _dynamicThreshold = _options.SilenceThreshold;
        _isCalibrated = false;
    }

    /// <summary>
    /// Calibrates the VAD by measuring ambient noise level.
    /// Should be called at startup with a few seconds of ambient audio.
    /// </summary>
    /// <param name="audioChunks">Audio chunks captured during calibration period.</param>
    /// <param name="multiplier">Multiplier for threshold above noise floor (default 1.5x).</param>
    public void Calibrate(IEnumerable<byte[]> audioChunks, float multiplier = 1.5f)
    {
        var rmsValues = audioChunks.Select(CalculateRms).ToList();
        
        if (rmsValues.Count == 0)
        {
            _logger.LogWarning("No audio chunks for calibration, using default threshold");
            return;
        }

        // Calculate average and max RMS of ambient noise
        float avgRms = rmsValues.Average();
        float maxRms = rmsValues.Max();
        
        // Set threshold above the noise floor
        // Use max + margin to avoid false triggers from occasional noise spikes
        float noiseFloor = Math.Max(avgRms, maxRms * 0.8f);
        _dynamicThreshold = noiseFloor * multiplier;
        
        // Ensure minimum threshold
        float minThreshold = 0.02f;
        // Ensure maximum threshold - if calibration captured speech/noise, cap it
        float maxThreshold = 0.15f;
        
        if (_dynamicThreshold < minThreshold)
        {
            _dynamicThreshold = minThreshold;
            _logger.LogWarning("Threshold too low, capping at minimum: {Min:F4}", minThreshold);
        }
        else if (_dynamicThreshold > maxThreshold)
        {
            _logger.LogWarning("Threshold too high ({Threshold:F4}), capping at maximum: {Max:F4}. Was there noise during calibration?", 
                _dynamicThreshold, maxThreshold);
            _dynamicThreshold = maxThreshold;
        }
        
        _isCalibrated = true;
        
        _logger.LogInformation("═══════════════════════════════════════════════════════════════");
        _logger.LogInformation("  🎚️ VAD CALIBRATION COMPLETE");
        _logger.LogInformation("  Samples: {Count}", rmsValues.Count);
        _logger.LogInformation("  Avg noise RMS: {Avg:F4}", avgRms);
        _logger.LogInformation("  Max noise RMS: {Max:F4}", maxRms);
        _logger.LogInformation("  Noise floor: {Floor:F4}", noiseFloor);
        _logger.LogInformation("  New threshold: {Threshold:F4} (multiplier: {Mult:F1}x)", _dynamicThreshold, multiplier);
        _logger.LogInformation("  Original threshold: {Original:F4}", _options.SilenceThreshold);
        _logger.LogInformation("═══════════════════════════════════════════════════════════════");
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
        return rms > _dynamicThreshold;
    }

    /// <summary>
    /// Detects if the audio chunk contains speech and returns the RMS value.
    /// </summary>
    /// <param name="pcmData">16-bit PCM audio data.</param>
    /// <returns>Tuple of (isSpeech, rmsValue).</returns>
    public (bool IsSpeech, float Rms) Analyze(byte[] pcmData)
    {
        float rms = CalculateRms(pcmData);
        return (rms > _dynamicThreshold, rms);
    }

    /// <summary>
    /// Gets the current silence threshold (dynamic after calibration).
    /// </summary>
    public float SilenceThreshold => _dynamicThreshold;
    
    /// <summary>
    /// Gets whether the VAD has been calibrated.
    /// </summary>
    public bool IsCalibrated => _isCalibrated;
}
