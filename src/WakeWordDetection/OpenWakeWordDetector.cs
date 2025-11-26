using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Collections.Concurrent;

namespace Olbrasoft.VoiceAssistant.WakeWordDetection;

/// <summary>
/// Wake word detector using OpenWakeWord ONNX models.
/// Implements Python OpenWakeWord streaming pipeline:
/// 1. Accumulate raw audio samples
/// 2. Compute melspectrogram for accumulated audio
/// 3. Extract embeddings from melspectrogram rolling window  
/// 4. Predict wake word from embeddings
/// </summary>
public class OpenWakeWordDetector : IWakeWordDetector
{
    private readonly ILogger<OpenWakeWordDetector> _logger;
    private readonly IAudioCapture _audioCapture;
    private readonly IWakeWordModelProvider _modelProvider;
    private readonly List<string> _modelPaths;
    private readonly string _melspecModelPath;
    private readonly string _embeddingModelPath;
    private readonly TimeSpan _debounceTime;
    
    private class ModelMetadata
    {
        public InferenceSession Session { get; set; } = null!;
        public string InputName { get; set; } = null!;
        public string OutputName { get; set; } = null!;
        public DateTime LastDetectionTime { get; set; } = DateTime.MinValue;
        public float Threshold { get; set; }
    }
    
    private readonly Dictionary<string, ModelMetadata> _wakeWordModels = new();
    private InferenceSession? _melspecSession;
    private InferenceSession? _embeddingSession;
    
    private bool _isListening;
    private bool _disposed;
    
    // Raw audio buffer (like Python's raw_data_buffer)
    private readonly List<short> _rawAudioBuffer = new();
    private int _accumulatedSamples = 0;
    private const int ChunkSize = 1280; // 80ms at 16kHz
    
    // Melspectrogram buffer (like Python's melspectrogram_buffer)
    private readonly List<float[]> _melspecBuffer = new();
    private const int MelspecBufferMaxLen = 970; // 10 seconds worth (~97 frames per second)
    
    // Feature/Embedding buffer (like Python's feature_buffer)
    private readonly List<float[]> _featureBuffer = new();
    private const int FeatureBufferMaxLen = 120; // ~10 seconds of feature buffer history
    
    private const int MelspecWindowSize = 76; // frames needed for embedding
    
    public event EventHandler<WakeWordDetectedEventArgs>? WakeWordDetected;
    
    public bool IsListening => _isListening;
    
    public OpenWakeWordDetector(
        ILogger<OpenWakeWordDetector> logger,
        IAudioCapture audioCapture,
        IWakeWordModelProvider modelProvider,
        List<string> modelPaths,
        string melspecModelPath,
        string embeddingModelPath,
        double debounceSeconds = 2.0)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _audioCapture = audioCapture ?? throw new ArgumentNullException(nameof(audioCapture));
        _modelProvider = modelProvider ?? throw new ArgumentNullException(nameof(modelProvider));
        _modelPaths = modelPaths ?? throw new ArgumentNullException(nameof(modelPaths));
        _melspecModelPath = melspecModelPath ?? throw new ArgumentNullException(nameof(melspecModelPath));
        _embeddingModelPath = embeddingModelPath ?? throw new ArgumentNullException(nameof(embeddingModelPath));
        _debounceTime = TimeSpan.FromSeconds(debounceSeconds);
        
        InitializeModels();
    }
    
    private void InitializeModels()
    {
        try
        {
            var sessionOptions = new SessionOptions
            {
                InterOpNumThreads = 1,
                IntraOpNumThreads = 1,
                ExecutionMode = ExecutionMode.ORT_SEQUENTIAL
            };
            
            _melspecSession = new InferenceSession(_melspecModelPath, sessionOptions);
            _embeddingSession = new InferenceSession(_embeddingModelPath, sessionOptions);
            
            // Get model configurations from provider
            var models = _modelProvider.GetModels(_modelPaths);
            
            // Load all wake word models
            foreach (var model in models)
            {
                var session = new InferenceSession(model.FilePath, sessionOptions);
                
                // Extract input and output tensor names from model metadata
                var inputName = session.InputMetadata.First().Key;
                var outputName = session.OutputMetadata.First().Key;
                
                _wakeWordModels[model.Name] = new ModelMetadata
                {
                    Session = session,
                    InputName = inputName,
                    OutputName = outputName,
                    LastDetectionTime = DateTime.MinValue,
                    Threshold = model.Threshold
                };
                
                _logger.LogInformation("  Loaded wake word: {WakeWordModel} (input={InputName}, output={OutputName}, threshold={Threshold}{ExplicitMarker})", 
                    Path.GetFileName(model.FilePath), inputName, outputName, model.Threshold, 
                    model.HasExplicitThreshold ? " [explicit]" : " [default]");
            }
            
            _logger.LogInformation("OpenWakeWord models loaded successfully");
            _logger.LogInformation("  Melspectrogram: {MelspecModel}", Path.GetFileName(_melspecModelPath));
            _logger.LogInformation("  Embedding: {EmbeddingModel}", Path.GetFileName(_embeddingModelPath));
            _logger.LogInformation("  Debounce: {Debounce}s", _debounceTime.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load ONNX models");
            throw;
        }
    }
    
    public async Task StartListeningAsync(CancellationToken cancellationToken = default)
    {
        if (_isListening)
        {
            _logger.LogWarning("Already listening");
            return;
        }
        
        _isListening = true;
        _audioCapture.AudioDataAvailable += OnAudioDataAvailable;
        
        await _audioCapture.StartCaptureAsync(cancellationToken);
        _logger.LogInformation("Started listening for wake word");
    }
    
    public async Task StopListeningAsync()
    {
        if (!_isListening)
        {
            return;
        }
        
        _isListening = false;
        _audioCapture.AudioDataAvailable -= OnAudioDataAvailable;
        await _audioCapture.StopCaptureAsync();
        
        _logger.LogInformation("Stopped listening for wake word");
    }
    
    private void OnAudioDataAvailable(object? sender, AudioDataEventArgs e)
    {
        try
        {
            // Add raw audio to buffer (like Python's _buffer_raw_data)
            _rawAudioBuffer.AddRange(e.AudioData);
            _accumulatedSamples += e.AudioData.Length;
            
            // Process when we have at least 1280 samples (like Python)
            if (_accumulatedSamples >= ChunkSize)
            {
                ProcessStreamingFeatures();
                _accumulatedSamples = 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing audio data");
        }
    }
    
    private void ProcessStreamingFeatures()
    {
        if (_wakeWordModels.Count == 0 || _melspecSession == null || _embeddingSession == null)
        {
            return;
        }
        
        try
        {
            // Step 1: Compute melspectrogram for ALL accumulated audio
            var melspecFrames = ComputeStreamingMelspectrogram();
            
            // Step 2: Calculate embeddings for each possible window
            // Python: for i in np.arange(self.accumulated_samples//1280-1, -1, -1)
            int numChunks = _rawAudioBuffer.Count / ChunkSize;
            
            for (int i = numChunks - 1; i >= 0; i--)
            {
                // Python: ndx = -8*i if i != 0 else len(melspectrogram_buffer)
                int ndx = i == 0 ? _melspecBuffer.Count : -8 * i;
                
                // Get 76 frames ending at ndx
                var startIdx = ndx - MelspecWindowSize;
                if (startIdx < 0)
                    startIdx = _melspecBuffer.Count + startIdx;
                    
                if (startIdx >= 0 && _melspecBuffer.Count >= MelspecWindowSize)
                {
                    var window = GetMelspecWindow(startIdx, ndx);
                    if (window != null && window.Count == MelspecWindowSize)
                    {
                        var embedding = ComputeEmbedding(window);
                        _featureBuffer.Add(embedding);
                        
                        // Trim feature buffer
                        if (_featureBuffer.Count > FeatureBufferMaxLen)
                        {
                            _featureBuffer.RemoveRange(0, _featureBuffer.Count - FeatureBufferMaxLen);
                        }
                    }
                }
            }
            
            // Clear raw audio buffer after processing
            _rawAudioBuffer.Clear();
            
            // Step 3: Predict wake word using latest features for ALL models
            if (_featureBuffer.Count >= 16)
            {
                foreach (var kvp in _wakeWordModels)
                {
                    var modelName = kvp.Key;
                    var metadata = kvp.Value;
                    var score = PredictWakeWord(metadata);
                    
                    if (score >= metadata.Threshold)
                    {
                        var now = DateTime.UtcNow;
                        if (now - metadata.LastDetectionTime >= _debounceTime)
                        {
                            metadata.LastDetectionTime = now;
                            OnWakeWordDetected(modelName, score);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in streaming features processing");
        }
    }
    
    private List<float[]> ComputeStreamingMelspectrogram()
    {
        // Convert entire raw buffer to float
        var audioFloat = new float[_rawAudioBuffer.Count];
        for (int i = 0; i < _rawAudioBuffer.Count; i++)
        {
            audioFloat[i] = _rawAudioBuffer[i];
        }
        
        // Create input tensor [1, samples]
        var inputTensor = new DenseTensor<float>(new[] { 1, _rawAudioBuffer.Count });
        for (int i = 0; i < _rawAudioBuffer.Count; i++)
        {
            inputTensor[0, i] = audioFloat[i];
        }
        
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input", inputTensor)
        };
        
        using var results = _melspecSession!.Run(inputs);
        var outputTensor = results.First().AsTensor<float>();
        
        // Output shape is [1, 1, time_frames, 32]
        var shape = outputTensor.Dimensions.ToArray();
        int timeFrames = shape[2];
        int numFeatures = shape[3]; // 32
        
        var newFrames = new List<float[]>();
        
        // Extract all frames and add to buffer
        for (int t = 0; t < timeFrames; t++)
        {
            var frame = new float[numFeatures];
            for (int j = 0; j < numFeatures; j++)
            {
                float value = outputTensor[0, 0, t, j];
                // Apply transform: x/10 + 2 (as per OpenWakeWord)
                frame[j] = value / 10f + 2f;
            }
            _melspecBuffer.Add(frame);
            newFrames.Add(frame);
        }
        
        // Trim melspec buffer
        if (_melspecBuffer.Count > MelspecBufferMaxLen)
        {
            _melspecBuffer.RemoveRange(0, _melspecBuffer.Count - MelspecBufferMaxLen);
        }
        
        return newFrames;
    }
    
    private List<float[]>? GetMelspecWindow(int startIdx, int endIdx)
    {
        if (endIdx <= 0)
            endIdx = _melspecBuffer.Count;
        if (startIdx < 0)
            return null;
        if (endIdx > _melspecBuffer.Count)
            return null;
        if (endIdx - startIdx != MelspecWindowSize)
            return null;
            
        return _melspecBuffer.GetRange(startIdx, MelspecWindowSize);
    }
    
    private float[] ComputeEmbedding(List<float[]> melspecWindow)
    {
        var numFeatures = melspecWindow[0].Length; // 32
        
        // Create input tensor [1, 76, 32, 1]
        var inputTensor = new DenseTensor<float>(new[] { 1, MelspecWindowSize, numFeatures, 1 });
        
        for (int i = 0; i < MelspecWindowSize; i++)
        {
            for (int j = 0; j < numFeatures; j++)
            {
                inputTensor[0, i, j, 0] = melspecWindow[i][j];
            }
        }
        
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_1", inputTensor)
        };
        
        using var results = _embeddingSession!.Run(inputs);
        return results.First().AsEnumerable<float>().ToArray();
    }
    
    private float PredictWakeWord(ModelMetadata metadata)
    {
        // Get last 16 feature frames
        var startIdx = Math.Max(0, _featureBuffer.Count - 16);
        var features = _featureBuffer.GetRange(startIdx, Math.Min(16, _featureBuffer.Count));
        
        // Pad if needed
        while (features.Count < 16)
        {
            features.Insert(0, new float[96]); // zero padding
        }
        
        // Create input tensor [1, 16, 96]
        var inputTensor = new DenseTensor<float>(new[] { 1, 16, 96 });
        
        for (int i = 0; i < 16; i++)
        {
            for (int j = 0; j < Math.Min(96, features[i].Length); j++)
            {
                inputTensor[0, i, j] = features[i][j];
            }
        }
        
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(metadata.InputName, inputTensor)
        };
        
        using var results = metadata.Session.Run(inputs);
        var output = results.First().AsEnumerable<float>().First();
        
        return output;
    }
    
    private void OnWakeWordDetected(string modelName, float score)
    {
        var args = new WakeWordDetectedEventArgs
        {
            DetectedWord = modelName,
            Confidence = score,
            DetectedAt = DateTime.UtcNow
        };
        WakeWordDetected?.Invoke(this, args);
    }
    
    public IReadOnlyCollection<string> GetWakeWords()
    {
        return _wakeWordModels.Keys.ToList();
    }
    
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        
        _disposed = true;
        
        foreach (var metadata in _wakeWordModels.Values)
        {
            metadata?.Session?.Dispose();
        }
        _wakeWordModels.Clear();
        
        _melspecSession?.Dispose();
        _embeddingSession?.Dispose();
        
        _audioCapture?.Dispose();
        
        _logger.LogInformation("OpenWakeWordDetector disposed");
    }
}
