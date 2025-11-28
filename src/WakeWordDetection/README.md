# WakeWordDetection

Core library for wake word detection using ONNX models.

## Features

- **Wake Word Detection** - Detects "Jarvis" or custom wake words
- **ONNX Runtime** - Fast inference using ONNX models
- **ALSA Audio** - Direct audio capture on Linux
- **Streaming Detection** - Real-time audio processing

## Components

### Audio Capture
- **IAudioCapture** - Interface for audio capture
- **AlsaAudioCapture** - ALSA-based audio capture for Linux
- **AudioDataEventArgs** - Event args for audio chunks

### Detection
- **IWakeWordDetector** - Interface for wake word detection
- **OnnxWakeWordDetector** - ONNX-based wake word detector

### Model Management
- **IModelProvider** - Interface for model loading
- **FileBasedModelProvider** - Loads ONNX models from filesystem

## Models

Wake word models are stored in `Models/` directory:
- `jarvis.onnx` - Jarvis wake word model

## Dependencies

- **Microsoft.ML.OnnxRuntime** - ONNX inference
- **Microsoft.Extensions.Logging.Abstractions** - Logging

## Usage

```csharp
var detector = new OnnxWakeWordDetector(modelPath, logger);
detector.WakeWordDetected += (sender, e) => {
    Console.WriteLine("Wake word detected!");
};
detector.Start();
```
