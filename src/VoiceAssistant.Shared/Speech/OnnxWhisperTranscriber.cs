using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Olbrasoft.VoiceAssistant.Shared.Speech;

/// <summary>
/// Microsoft ONNX Runtime based Whisper transcriber with full CUDA GPU support.
/// Uses separate encoder/decoder ONNX models (sherpa-onnx format).
/// </summary>
public class OnnxWhisperTranscriber : ISpeechTranscriber
{
    private readonly ILogger<OnnxWhisperTranscriber> _logger;
    private readonly string _modelPath;
    private readonly string _language;
    private InferenceSession? _encoderSession;
    private InferenceSession? _decoderSession;
    private TokenDecoder? _tokenDecoder;
    private AudioPreprocessor? _audioPreprocessor;
    private bool _disposed;
    
    // Model-specific parameters (detected from model files)
    private int _nLayers;
    private int _hiddenDim;
    private string _modelSize = "unknown";

    // Whisper audio configuration
    private const int SampleRate = 16000;
    private const int AudioLength = SampleRate * 30; // 30 seconds
    private const int NMels = 80;
    private const int ChunkLength = 3000;

    /// <summary>
    /// Initializes a new instance of the <see cref="OnnxWhisperTranscriber"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="modelPath">Path to directory containing encoder.onnx, decoder.onnx, and tokens.txt.</param>
    /// <param name="language">Language code (e.g., "cs" for Czech, "en" for English).</param>
    public OnnxWhisperTranscriber(ILogger<OnnxWhisperTranscriber> logger, string modelPath, string language = "cs")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _modelPath = modelPath ?? throw new ArgumentNullException(nameof(modelPath));
        _language = language ?? throw new ArgumentNullException(nameof(language));

        if (!Directory.Exists(_modelPath))
        {
            throw new DirectoryNotFoundException($"ONNX Whisper model directory not found: {_modelPath}");
        }
    }

    /// <inheritdoc/>
    public string Language => _language;

    /// <summary>
    /// Initializes the ONNX Runtime sessions with CUDA GPU support (lazy initialization).
    /// </summary>
    private void Initialize()
    {
        if (_encoderSession != null && _decoderSession != null)
            return;

        try
        {
            _logger.LogInformation("Loading ONNX Whisper models from: {ModelPath}", _modelPath);

            // Auto-detect model size by looking for encoder files
            string encoderPath, decoderPath, tokensPath;
            
            var sizes = new[] { "small", "base", "tiny", "medium", "large" };
            _modelSize = "unknown";
            
            foreach (var size in sizes)
            {
                var testEncoder = Path.Combine(_modelPath, $"{size}-encoder.onnx");
                if (File.Exists(testEncoder))
                {
                    _modelSize = size;
                    break;
                }
            }
            
            if (_modelSize == "unknown")
                throw new FileNotFoundException($"No Whisper encoder model found in: {_modelPath}");
            
            encoderPath = Path.Combine(_modelPath, $"{_modelSize}-encoder.onnx");
            decoderPath = Path.Combine(_modelPath, $"{_modelSize}-decoder.onnx");
            tokensPath = Path.Combine(_modelPath, $"{_modelSize}-tokens.txt");
            
            // Set model-specific parameters
            (_nLayers, _hiddenDim) = _modelSize switch
            {
                "tiny" => (4, 384),
                "base" => (6, 512),
                "small" => (12, 768),
                "medium" => (24, 1024),
                "large" => (32, 1280),
                _ => (24, 1024)
            };

            if (!File.Exists(encoderPath))
                throw new FileNotFoundException($"Encoder model not found: {encoderPath}");
            if (!File.Exists(decoderPath))
                throw new FileNotFoundException($"Decoder model not found: {decoderPath}");
            if (!File.Exists(tokensPath))
                throw new FileNotFoundException($"Tokens file not found: {tokensPath}");

            // Create session options with CUDA
            var sessionOptions = CreateSessionOptions();

            // Load encoder and decoder
            _encoderSession = new InferenceSession(encoderPath, sessionOptions);
            _decoderSession = new InferenceSession(decoderPath, sessionOptions);

            // Load token decoder and audio preprocessor with official mel filters
            _tokenDecoder = new TokenDecoder(tokensPath);
            var melFiltersPath = Path.Combine(_modelPath, "whisper_mel_filters.bin");
            _audioPreprocessor = new AudioPreprocessor(melFiltersPath);

            _logger.LogInformation("ONNX Whisper {Size} model loaded (layers: {Layers}, hidden: {Hidden}, mel filters: {Filters})", 
                _modelSize, _nLayers, _hiddenDim,
                File.Exists(melFiltersPath) ? "official" : "generated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load ONNX Whisper models");
            throw;
        }
    }

    /// <summary>
    /// Creates SessionOptions with CUDA GPU acceleration.
    /// </summary>
    private SessionOptions CreateSessionOptions()
    {
        var options = new SessionOptions();

        // Try CUDA first
        try
        {
            options.AppendExecutionProvider_CUDA(0); // Device ID 0
            _logger.LogInformation("CUDA execution provider enabled for GPU acceleration");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enable CUDA, falling back to CPU");
        }

        // Graph optimizations
        options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
        options.EnableMemoryPattern = true;
        options.EnableCpuMemArena = true;

        return options;
    }

    /// <summary>
    /// Converts PCM byte array to float32 samples normalized to [-1.0, 1.0].
    /// </summary>
    private static float[] ConvertPcmToFloat32(byte[] pcmData)
    {
        var samples = new float[pcmData.Length / 2];
        for (int i = 0; i < samples.Length; i++)
        {
            short sample = BitConverter.ToInt16(pcmData, i * 2);
            samples[i] = sample / 32768.0f;
        }
        return samples;
    }

    /// <summary>
    /// Strips WAV header if present (first 44 bytes).
    /// </summary>
    private static byte[] StripWavHeader(byte[] audioData)
    {
        if (audioData.Length > 44 &&
            audioData[0] == 'R' && audioData[1] == 'I' &&
            audioData[2] == 'F' && audioData[3] == 'F')
        {
            var pcmData = new byte[audioData.Length - 44];
            Array.Copy(audioData, 44, pcmData, 0, pcmData.Length);
            return pcmData;
        }
        return audioData;
    }

    /// <summary>
    /// Pads or trims audio to exactly 30 seconds (480000 samples at 16kHz).
    /// </summary>
    private static float[] PadOrTrimAudio(float[] samples)
    {
        if (samples.Length == AudioLength)
            return samples;

        var result = new float[AudioLength];
        int copyLength = Math.Min(samples.Length, AudioLength);
        Array.Copy(samples, 0, result, 0, copyLength);
        return result;
    }

    /// <inheritdoc/>
    public async Task<TranscriptionResult> TranscribeAsync(byte[] audioData, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(OnnxWhisperTranscriber));

        try
        {
            Initialize();

            _logger.LogDebug("Starting transcription... (audio size: {Size} bytes)", audioData.Length);

            // Strip WAV header if present
            var pcmData = StripWavHeader(audioData);

            // Convert PCM to float32 samples
            var allSamples = ConvertPcmToFloat32(pcmData);
            
            _logger.LogDebug("Total audio samples: {Count} ({Seconds:F1}s)", 
                allSamples.Length, allSamples.Length / (float)SampleRate);

            // Process in 30-second chunks with overlap
            const int chunkSamples = AudioLength; // 30 seconds = 480000 samples
            const int overlapSamples = SampleRate * 1; // 1 second overlap
            
            var transcriptions = new List<string>();
            string? previousTranscription = null; // For condition_on_previous_text
            int position = 0;
            int chunkIndex = 0;
            
            while (position < allSamples.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Extract chunk
                int remainingSamples = allSamples.Length - position;
                int chunkSize = Math.Min(chunkSamples, remainingSamples);
                
                var chunk = new float[chunkSamples]; // Always 30s (padded if needed)
                Array.Copy(allSamples, position, chunk, 0, chunkSize);
                
                _logger.LogDebug("Processing chunk {Index} (position: {Pos}, size: {Size}, context: {Context})", 
                    chunkIndex, position, chunkSize, 
                    previousTranscription != null ? $"'{previousTranscription.Substring(0, Math.Min(30, previousTranscription.Length))}...'" : "none");
                
                // Transcribe chunk with optional previous context
                var chunkTranscription = TranscribeChunk(chunk, previousTranscription);
                
                if (!string.IsNullOrWhiteSpace(chunkTranscription))
                {
                    transcriptions.Add(chunkTranscription);
                    // Use last 224 characters as context for next chunk (Whisper uses max 224 tokens for context)
                    previousTranscription = chunkTranscription.Length > 224 
                        ? chunkTranscription.Substring(chunkTranscription.Length - 224)
                        : chunkTranscription;
                }
                
                // Move to next chunk (with overlap for continuity)
                position += chunkSamples - overlapSamples;
                chunkIndex++;
                
                // Safety limit - max 10 chunks (5 minutes)
                if (chunkIndex >= 10)
                {
                    _logger.LogWarning("Audio too long, stopping at chunk {Index}", chunkIndex);
                    break;
                }
            }
            
            // Combine all transcriptions
            var transcription = string.Join(" ", transcriptions);

            if (string.IsNullOrWhiteSpace(transcription))
            {
                _logger.LogWarning("Transcription result is empty");
                return new TranscriptionResult("No speech detected");
            }

            const float confidence = 1.0f;
            _logger.LogInformation("Transcription successful ({Chunks} chunks): {Text}", 
                transcriptions.Count, transcription);

            return new TranscriptionResult(transcription, confidence);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Transcription was cancelled");
            return new TranscriptionResult("Transcription cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transcription failed");
            return new TranscriptionResult($"Transcription error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Transcribes a single 30-second audio chunk.
    /// </summary>
    /// <param name="audioInput">Audio samples (30 seconds at 16kHz).</param>
    /// <param name="previousContext">Optional previous transcription for context conditioning.</param>
    private string TranscribeChunk(float[] audioInput, string? previousContext = null)
    {
        // Create mel spectrogram input tensor [1, 80, 3000]
        var melSpectrogram = ComputeMelSpectrogram(audioInput);

        // Run encoder
        var encoderInputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("mel", melSpectrogram)
        };
        
        _logger.LogDebug("Mel spectrogram shape: [{Dims}]", string.Join(", ", melSpectrogram.Dimensions.ToArray()));

        using var encoderResults = _encoderSession!.Run(encoderInputs);
        
        // Get encoder outputs: cross-attention key and value tensors
        var crossK = encoderResults.First(r => r.Name == "n_layer_cross_k").AsTensor<float>();
        var crossV = encoderResults.First(r => r.Name == "n_layer_cross_v").AsTensor<float>();
        
        _logger.LogDebug("Encoder output crossK shape: [{Dims}]", string.Join(", ", crossK.Dimensions.ToArray()));
        _logger.LogDebug("Encoder output crossV shape: [{Dims}]", string.Join(", ", crossV.Dimensions.ToArray()));

        // Autoregressive decoding with optional context
        return DecodeGreedy(crossK, crossV, previousContext);
    }

    /// <summary>
    /// Computes mel spectrogram from audio samples using AudioPreprocessor.
    /// </summary>
    private DenseTensor<float> ComputeMelSpectrogram(float[] samples)
    {
        if (_audioPreprocessor == null)
            throw new InvalidOperationException("AudioPreprocessor not initialized");

        // Compute mel spectrogram [1, 80, frames]
        var mel = _audioPreprocessor.ComputeMelSpectrogram(samples);
        
        // Convert to DenseTensor
        int batch = mel.GetLength(0);
        int mels = mel.GetLength(1);
        int frames = mel.GetLength(2);
        
        var flatArray = new float[batch * mels * frames];
        int idx = 0;
        for (int b = 0; b < batch; b++)
        {
            for (int m = 0; m < mels; m++)
            {
                for (int f = 0; f < frames; f++)
                {
                    flatArray[idx++] = mel[b, m, f];
                }
            }
        }
        
        return new DenseTensor<float>(flatArray, new[] { batch, mels, frames });
    }

    /// <summary>
    /// Autoregressive decoding with KV caching.
    /// Processes tokens one by one as expected by sherpa-onnx models.
    /// </summary>
    /// <param name="crossK">Cross-attention keys from encoder.</param>
    /// <param name="crossV">Cross-attention values from encoder.</param>
    /// <param name="previousContext">Optional previous transcription for context conditioning.</param>
    private string DecodeGreedy(Tensor<float> crossK, Tensor<float> crossV, string? previousContext = null)
    {
        if (_decoderSession == null || _tokenDecoder == null)
            throw new InvalidOperationException("Decoder not initialized");

        const int maxTokens = 448; // Max sequence length
        const int endOfTextToken = 50257;
        const int startOfTranscriptToken = 50258;
        const int translateToken = 50358;
        const int transcribeToken = 50359;
        const int noSpeechToken = 50362;
        const int notimestampsToken = 50363;
        const int timestampBeginToken = 50364; // <|0.00|> - first timestamp token
        const int timestampEndToken = 50864;   // <|30.00|> - last timestamp token
        
        // Decoding parameters for better quality
        const float repetitionPenalty = 1.1f; // Penalize repeated tokens
        const float frequencyPenalty = 0.0f;  // Penalize based on frequency
        
        // Get start tokens - with or without context
        int[] startTokens;
        if (!string.IsNullOrWhiteSpace(previousContext))
        {
            startTokens = _tokenDecoder.GetStartTokensWithContext(previousContext, _language);
            _logger.LogDebug("Using context-conditioned start tokens ({Count} tokens)", startTokens.Length);
        }
        else
        {
            startTokens = TokenDecoder.GetStartTokens(_language);
        }
        var allTokens = new List<int>();
        
        // Token frequency counter for frequency penalty
        var tokenCounts = new Dictionary<int, int>();
        
        _logger.LogDebug("Start tokens: [{Tokens}] (first 10)", string.Join(", ", startTokens.Take(10)));
        _logger.LogDebug("crossK shape: [{Dims}]", string.Join(", ", crossK.Dimensions.ToArray()));
        
        // Initialize KV caches with model-specific dimensions
        var selfKCache = new DenseTensor<float>(new[] { _nLayers, 1, maxTokens, _hiddenDim });
        var selfVCache = new DenseTensor<float>(new[] { _nLayers, 1, maxTokens, _hiddenDim });
        int offset = 0;
        
        // Process start tokens one by one first
        foreach (var token in startTokens)
        {
            allTokens.Add(token);
            (selfKCache, selfVCache) = RunDecoderStep(token, selfKCache, selfVCache, crossK, crossV, offset);
            offset++;
        }
        
        _logger.LogDebug("Start tokens processed ({Count}), generating...", startTokens.Length);
        
        // Repetition detection: track last N tokens for ngram matching
        const int repeatWindow = 4; // Check for repeated 4-grams
        const int maxRepeats = 3;   // Stop if same 4-gram repeats 3 times
        
        // Generate new tokens
        for (int step = 0; step < maxTokens - startTokens.Length; step++)
        {
            // Get logits for last token
            var tokensTensor = new DenseTensor<long>(new[] { 1, 1 });
            tokensTensor[0, 0] = allTokens[allTokens.Count - 1];
            
            var offsetTensor = new DenseTensor<long>(new[] { 1 });
            offsetTensor[0] = offset;
            
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("tokens", tokensTensor),
                NamedOnnxValue.CreateFromTensor("in_n_layer_self_k_cache", selfKCache),
                NamedOnnxValue.CreateFromTensor("in_n_layer_self_v_cache", selfVCache),
                NamedOnnxValue.CreateFromTensor("n_layer_cross_k", crossK),
                NamedOnnxValue.CreateFromTensor("n_layer_cross_v", crossV),
                NamedOnnxValue.CreateFromTensor("offset", offsetTensor)
            };
            
            using var results = _decoderSession.Run(inputs);
            var logits = results.First(r => r.Name == "logits").AsTensor<float>();
            
            // Apply token suppression for special tokens
            // Suppress timestamp tokens
            for (int t = timestampBeginToken; t <= timestampEndToken && t < 51865; t++)
            {
                logits[0, 0, t] = float.NegativeInfinity;
            }
            // Suppress other special tokens that shouldn't appear in output
            logits[0, 0, startOfTranscriptToken] = float.NegativeInfinity;
            logits[0, 0, translateToken] = float.NegativeInfinity;
            logits[0, 0, transcribeToken] = float.NegativeInfinity;
            logits[0, 0, noSpeechToken] = float.NegativeInfinity;
            logits[0, 0, notimestampsToken] = float.NegativeInfinity;
            // Suppress language tokens (50259-50357)
            for (int t = 50259; t <= 50357; t++)
            {
                logits[0, 0, t] = float.NegativeInfinity;
            }
            
            // Apply repetition penalty to already generated tokens
            if (repetitionPenalty != 1.0f)
            {
                foreach (var prevToken in allTokens)
                {
                    if (prevToken < 51865 && prevToken >= 0)
                    {
                        float logit = logits[0, 0, prevToken];
                        if (logit > 0)
                            logits[0, 0, prevToken] = logit / repetitionPenalty;
                        else
                            logits[0, 0, prevToken] = logit * repetitionPenalty;
                    }
                }
            }
            
            // Find best token (logits shape is [1, 1, vocab_size])
            float maxLogit = float.MinValue;
            int bestToken = 0;
            for (int v = 0; v < 51865; v++)
            {
                float logit = logits[0, 0, v];
                if (logit > maxLogit)
                {
                    maxLogit = logit;
                    bestToken = v;
                }
            }
            
            // Check for end of text
            if (bestToken == endOfTextToken)
            {
                _logger.LogDebug("EOT reached at step {Step}", step);
                break;
            }
            
            allTokens.Add(bestToken);
            
            // Track token frequency
            if (!tokenCounts.TryAdd(bestToken, 1))
                tokenCounts[bestToken]++;
            
            // Check for repetition (n-gram based)
            if (DetectRepetition(allTokens, repeatWindow, maxRepeats))
            {
                _logger.LogWarning("Repetition detected at step {Step}, stopping generation", step);
                // Remove the repeated tokens
                int tokensToRemove = repeatWindow * (maxRepeats - 1);
                if (allTokens.Count > startTokens.Length + tokensToRemove)
                {
                    allTokens.RemoveRange(allTokens.Count - tokensToRemove, tokensToRemove);
                }
                break;
            }
            
            // Update KV caches
            var outK = results.First(r => r.Name == "out_n_layer_self_k_cache").AsTensor<float>();
            var outV = results.First(r => r.Name == "out_n_layer_self_v_cache").AsTensor<float>();
            selfKCache = new DenseTensor<float>(outK.ToArray(), outK.Dimensions.ToArray());
            selfVCache = new DenseTensor<float>(outV.ToArray(), outV.Dimensions.ToArray());
            
            offset++;
            
            // Log progress every 50 tokens
            if (step > 0 && step % 50 == 0)
            {
                _logger.LogDebug("Generated {Count} tokens...", step);
            }
        }
        
        _logger.LogDebug("Total tokens generated: {Count} (including {Start} start tokens)", 
            allTokens.Count, startTokens.Length);
        
        // Decode tokens to text (skip start tokens)
        var textTokens = allTokens.Skip(startTokens.Length).ToArray();
        return _tokenDecoder.Decode(textTokens);
    }
    
    /// <summary>
    /// Detects if the last n-grams are repeating.
    /// </summary>
    private static bool DetectRepetition(List<int> tokens, int windowSize, int maxRepeats)
    {
        if (tokens.Count < windowSize * maxRepeats)
            return false;
        
        // Get the last n-gram
        var lastNgram = new int[windowSize];
        for (int i = 0; i < windowSize; i++)
        {
            lastNgram[i] = tokens[tokens.Count - windowSize + i];
        }
        
        // Count how many times this n-gram appears in the last (windowSize * maxRepeats) tokens
        int repeatCount = 0;
        for (int start = tokens.Count - windowSize * maxRepeats; start <= tokens.Count - windowSize; start += windowSize)
        {
            bool matches = true;
            for (int i = 0; i < windowSize; i++)
            {
                if (tokens[start + i] != lastNgram[i])
                {
                    matches = false;
                    break;
                }
            }
            if (matches) repeatCount++;
        }
        
        return repeatCount >= maxRepeats;
    }
    
    /// <summary>
    /// Runs a single decoder step for one token.
    /// </summary>
    private (DenseTensor<float> k, DenseTensor<float> v) RunDecoderStep(
        int token, 
        DenseTensor<float> selfKCache, 
        DenseTensor<float> selfVCache,
        Tensor<float> crossK, 
        Tensor<float> crossV, 
        int offset)
    {
        var tokensTensor = new DenseTensor<long>(new[] { 1, 1 });
        tokensTensor[0, 0] = token;
        
        var offsetTensor = new DenseTensor<long>(new[] { 1 });
        offsetTensor[0] = offset;
        
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("tokens", tokensTensor),
            NamedOnnxValue.CreateFromTensor("in_n_layer_self_k_cache", selfKCache),
            NamedOnnxValue.CreateFromTensor("in_n_layer_self_v_cache", selfVCache),
            NamedOnnxValue.CreateFromTensor("n_layer_cross_k", crossK),
            NamedOnnxValue.CreateFromTensor("n_layer_cross_v", crossV),
            NamedOnnxValue.CreateFromTensor("offset", offsetTensor)
        };
        
        using var results = _decoderSession!.Run(inputs);
        
        var outK = results.First(r => r.Name == "out_n_layer_self_k_cache").AsTensor<float>();
        var outV = results.First(r => r.Name == "out_n_layer_self_v_cache").AsTensor<float>();
        
        return (
            new DenseTensor<float>(outK.ToArray(), outK.Dimensions.ToArray()),
            new DenseTensor<float>(outV.ToArray(), outV.Dimensions.ToArray())
        );
    }

    /// <inheritdoc/>
    public async Task<TranscriptionResult> TranscribeAsync(Stream audioStream, CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        await audioStream.CopyToAsync(memoryStream, cancellationToken);
        return await TranscribeAsync(memoryStream.ToArray(), cancellationToken);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        _encoderSession?.Dispose();
        _decoderSession?.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);

        _logger.LogDebug("OnnxWhisperTranscriber disposed");
    }
}
