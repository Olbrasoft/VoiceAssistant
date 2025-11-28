using NAudio.Dsp;
using System;
using System.IO;

namespace Olbrasoft.VoiceAssistant.Shared.Speech;

/// <summary>
/// Whisper audio preprocessing - converts PCM audio to mel spectrogram.
/// Based on OpenAI Whisper audio.py constants and preprocessing.
/// </summary>
public class AudioPreprocessor
{
    // Whisper audio hyperparameters (fixed by the model)
    private const int SampleRate = 16000;
    private const int NFFt = 400;
    private const int HopLength = 160;
    private const int NMels = 80;
    private const int ChunkLength = 30; // seconds
    private const int NSamples = ChunkLength * SampleRate; // 480000
    private const int ExpectedFrames = 3000; // For 30 seconds
    
    private readonly float[,] _melFilterbank;
    private readonly bool _useOfficialFilters;

    public AudioPreprocessor()
    {
        // Try to find official filters in common locations
        var possiblePaths = new[]
        {
            "whisper_mel_filters.bin",
            Path.Combine(AppContext.BaseDirectory, "whisper_mel_filters.bin"),
            Path.Combine(AppContext.BaseDirectory, "models", "whisper_mel_filters.bin"),
        };
        
        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                _melFilterbank = LoadMelFilterbank(path);
                _useOfficialFilters = true;
                return;
            }
        }
        
        _melFilterbank = CreateMelFilterbank();
        _useOfficialFilters = false;
    }
    
    /// <summary>
    /// Creates AudioPreprocessor with official Whisper mel filterbank.
    /// </summary>
    /// <param name="melFiltersPath">Path to binary file with mel filters (80x201 float32)</param>
    public AudioPreprocessor(string melFiltersPath)
    {
        if (File.Exists(melFiltersPath))
        {
            _melFilterbank = LoadMelFilterbank(melFiltersPath);
            _useOfficialFilters = true;
        }
        else
        {
            _melFilterbank = CreateMelFilterbank();
            _useOfficialFilters = false;
        }
    }

    /// <summary>
    /// Converts float32 audio samples to mel spectrogram [1, 80, frames].
    /// </summary>
    /// <param name="samples">Audio samples (float32, normalized to [-1, 1])</param>
    /// <returns>Mel spectrogram tensor [1, 80, frames]</returns>
    public float[,,] ComputeMelSpectrogram(float[] samples)
    {
        // Pad or trim to 30 seconds (480000 samples)
        var paddedSamples = PadOrTrim(samples, NSamples);
        
        // Compute STFT
        var stft = ComputeSTFT(paddedSamples);
        
        // Convert to mel scale
        var mel = ApplyMelFilterbank(stft);
        
        // Log10 mel spectrogram (Whisper uses log10, not ln!)
        const float epsilon = 1e-10f;
        float maxLogMel = float.MinValue;
        
        for (int i = 0; i < mel.GetLength(0); i++)
        {
            for (int j = 0; j < mel.GetLength(1); j++)
            {
                mel[i, j] = MathF.Log10(MathF.Max(mel[i, j], epsilon));
                if (mel[i, j] > maxLogMel)
                    maxLogMel = mel[i, j];
            }
        }
        
        // Whisper normalization: clamp to max - 8.0, then (x + 4) / 4
        float minLogMel = maxLogMel - 8.0f;
        for (int i = 0; i < mel.GetLength(0); i++)
        {
            for (int j = 0; j < mel.GetLength(1); j++)
            {
                mel[i, j] = MathF.Max(mel[i, j], minLogMel);
                mel[i, j] = (mel[i, j] + 4.0f) / 4.0f;
            }
        }
        
        // Reshape to [1, 80, frames]
        int frames = mel.GetLength(1);
        var result = new float[1, NMels, frames];
        for (int i = 0; i < NMels; i++)
        {
            for (int j = 0; j < frames; j++)
            {
                result[0, i, j] = mel[i, j];
            }
        }
        
        return result;
    }

    private float[] PadOrTrim(float[] samples, int targetLength)
    {
        if (samples.Length == targetLength)
            return samples;
            
        var result = new float[targetLength];
        
        if (samples.Length > targetLength)
        {
            // Trim
            Array.Copy(samples, result, targetLength);
        }
        else
        {
            // Pad with zeros
            Array.Copy(samples, result, samples.Length);
        }
        
        return result;
    }

    /// <summary>
    /// Compute Short-Time Fourier Transform.
    /// Returns power spectrogram [n_freq_bins, n_frames].
    /// Whisper expects exactly 3000 frames for 30 seconds of audio.
    /// Uses center=True with reflect padding like PyTorch.
    /// </summary>
    private float[,] ComputeSTFT(float[] samples)
    {
        // Center padding: pad NFFt/2 on both sides using reflect mode
        int padSize = NFFt / 2;
        var paddedSamples = new float[samples.Length + 2 * padSize];
        
        // Reflect padding at the start
        for (int i = 0; i < padSize; i++)
        {
            paddedSamples[padSize - 1 - i] = samples[Math.Min(i + 1, samples.Length - 1)];
        }
        
        // Copy original samples
        Array.Copy(samples, 0, paddedSamples, padSize, samples.Length);
        
        // Reflect padding at the end
        for (int i = 0; i < padSize; i++)
        {
            int srcIdx = samples.Length - 2 - i;
            if (srcIdx < 0) srcIdx = 0;
            paddedSamples[padSize + samples.Length + i] = samples[srcIdx];
        }
        
        var spectrogram = new float[NFFt / 2 + 1, ExpectedFrames];
        var fftBuffer = new Complex[NFFt];
        var window = CreateHannWindow(NFFt);
        
        for (int frameIdx = 0; frameIdx < ExpectedFrames; frameIdx++)
        {
            int startSample = frameIdx * HopLength;
            
            // Apply window and prepare FFT input
            for (int i = 0; i < NFFt; i++)
            {
                int idx = startSample + i;
                fftBuffer[i].X = idx < paddedSamples.Length ? paddedSamples[idx] * window[i] : 0;
                fftBuffer[i].Y = 0;
            }
            
            // FFT
            FastFourierTransform.FFT(true, (int)Math.Log(NFFt, 2), fftBuffer);
            
            // Compute power spectrum (magnitude squared)
            // NAudio FFT returns normalized values, scale back
            for (int i = 0; i <= NFFt / 2; i++)
            {
                float re = fftBuffer[i].X * NFFt;
                float im = fftBuffer[i].Y * NFFt;
                float magnitude = MathF.Sqrt(re * re + im * im);
                spectrogram[i, frameIdx] = magnitude * magnitude;
            }
        }
        
        return spectrogram;
    }

    private float[] CreateHannWindow(int size)
    {
        // Periodic Hann window (PyTorch default)
        var window = new float[size];
        for (int i = 0; i < size; i++)
        {
            window[i] = 0.5f * (1.0f - MathF.Cos(2.0f * MathF.PI * i / size));
        }
        return window;
    }

    /// <summary>
    /// Apply mel filterbank to power spectrogram.
    /// </summary>
    private float[,] ApplyMelFilterbank(float[,] spectrogram)
    {
        int nFreqBins = spectrogram.GetLength(0);
        int nFrames = spectrogram.GetLength(1);
        var mel = new float[NMels, nFrames];
        
        for (int melIdx = 0; melIdx < NMels; melIdx++)
        {
            for (int frameIdx = 0; frameIdx < nFrames; frameIdx++)
            {
                float sum = 0;
                for (int freqIdx = 0; freqIdx < nFreqBins; freqIdx++)
                {
                    sum += spectrogram[freqIdx, frameIdx] * _melFilterbank[melIdx, freqIdx];
                }
                mel[melIdx, frameIdx] = sum;
            }
        }
        
        return mel;
    }
    
    /// <summary>
    /// Loads official Whisper mel filterbank from binary file.
    /// </summary>
    private float[,] LoadMelFilterbank(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var fb = new float[NMels, NFFt / 2 + 1];
        Buffer.BlockCopy(bytes, 0, fb, 0, bytes.Length);
        return fb;
    }

    /// <summary>
    /// Creates mel filterbank matrix [n_mels, n_freq_bins].
    /// Based on librosa mel filterbank implementation.
    /// Note: For best results, use official Whisper filters via LoadMelFilterbank.
    /// </summary>
    private float[,] CreateMelFilterbank()
    {
        int nFreqBins = NFFt / 2 + 1; // 201 bins
        var filterbank = new float[NMels, nFreqBins];
        
        // Mel scale conversion
        float melMin = HzToMel(0);
        float melMax = HzToMel(SampleRate / 2.0f);
        
        // Create mel points
        var melPoints = new float[NMels + 2];
        for (int i = 0; i < NMels + 2; i++)
        {
            melPoints[i] = melMin + (melMax - melMin) * i / (NMels + 1);
        }
        
        // Convert mel points back to Hz
        var hzPoints = new float[NMels + 2];
        for (int i = 0; i < NMels + 2; i++)
        {
            hzPoints[i] = MelToHz(melPoints[i]);
        }
        
        // Convert Hz to FFT bin indices
        var binPoints = new int[NMels + 2];
        for (int i = 0; i < NMels + 2; i++)
        {
            binPoints[i] = (int)Math.Floor((NFFt + 1) * hzPoints[i] / SampleRate);
        }
        
        // Create triangular filters
        for (int melIdx = 0; melIdx < NMels; melIdx++)
        {
            int leftBin = binPoints[melIdx];
            int centerBin = binPoints[melIdx + 1];
            int rightBin = binPoints[melIdx + 2];
            
            // Rising slope
            for (int bin = leftBin; bin < centerBin && bin < nFreqBins; bin++)
            {
                if (centerBin > leftBin)
                {
                    filterbank[melIdx, bin] = (float)(bin - leftBin) / (centerBin - leftBin);
                }
            }
            
            // Falling slope
            for (int bin = centerBin; bin < rightBin && bin < nFreqBins; bin++)
            {
                if (rightBin > centerBin)
                {
                    filterbank[melIdx, bin] = (float)(rightBin - bin) / (rightBin - centerBin);
                }
            }
        }
        
        return filterbank;
    }

    private static float HzToMel(float hz)
    {
        return 2595.0f * MathF.Log10(1.0f + hz / 700.0f);
    }

    private static float MelToHz(float mel)
    {
        return 700.0f * (MathF.Pow(10.0f, mel / 2595.0f) - 1.0f);
    }
}
