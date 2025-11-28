# VoiceAssistant

Voice assistant platform with push-to-talk dictation, wake word detection, and orchestration.

## Projects

### 1. PushToTalkDictation
Push-to-talk dictation service using Caps Lock key with GPU-accelerated Whisper.

**Technology:** .NET 10, ONNX Runtime, CUDA GPU, ALSA audio

**Features:**
- Hold Caps Lock to record, release to transcribe
- Custom ONNX Whisper implementation with CUDA GPU
- Supports small/medium Whisper models
- Chunking for audio longer than 30 seconds
- Czech language optimized

**Location:** `src/PushToTalkDictation/`, `src/PushToTalkDictation.Service/`

**Key files:**
- `VoiceAssistant.Shared/Speech/OnnxWhisperTranscriber.cs` - ONNX Whisper with GPU
- `VoiceAssistant.Shared/Speech/AudioPreprocessor.cs` - Mel spectrogram
- `VoiceAssistant.Shared/Speech/TokenDecoder.cs` - BPE token decoder

### 2. WakeWordDetection
Offline wake word detection service using OpenWakeWord and ONNX models.

**Technology:** .NET 10, ASP.NET Core, SignalR WebSocket, ALSA audio

**Features:**
- Offline wake word detection (hey_jarvis, alexa, hey_mycroft, hey_rhasspy)
- Per-model threshold configuration
- Real-time WebSocket notifications via SignalR
- REST API for status and testing

**Location:** `src/WakeWordDetection/`, `src/WakeWordDetection.Service/`

### 3. Orchestration
Voice assistant orchestrator that coordinates wake word detection and audio responses.

**Technology:** .NET 10 Worker Service, SignalR Client

**Features:**
- Connects to WakeWordDetection via SignalR WebSocket
- Plays wake-word specific audio responses

**Location:** `src/Orchestration/`

### 4. EdgeTtsWebSocketServer
Text-to-Speech server using Microsoft Edge TTS.

**Technology:** .NET 10, WebSocket

**Location:** `src/EdgeTtsWebSocketServer/`

## Structure

```
VoiceAssistant/
├── src/
│   ├── VoiceAssistant.Shared/           # Shared library (ONNX Whisper, etc.)
│   │   └── Speech/
│   │       ├── OnnxWhisperTranscriber.cs
│   │       ├── AudioPreprocessor.cs
│   │       └── TokenDecoder.cs
│   ├── PushToTalkDictation/             # Push-to-talk core library
│   ├── PushToTalkDictation.Service/     # Systemd service
│   ├── WakeWordDetection/               # Wake word detection library
│   ├── WakeWordDetection.Service/       # ASP.NET Core service
│   ├── Orchestration/                   # Voice assistant orchestrator
│   └── EdgeTtsWebSocketServer/          # TTS server
├── tests/
│   ├── VoiceAssistant.Shared.Tests/
│   ├── WakeWordDetection.Tests/
│   └── WakeWordDetection.Service.Tests/
└── VoiceAssistant.sln
```

## Build & Deploy

### PushToTalkDictation

```bash
# Build
dotnet build src/PushToTalkDictation.Service/PushToTalkDictation.Service.csproj -c Release

# Publish
dotnet publish src/PushToTalkDictation.Service/PushToTalkDictation.Service.csproj \
  -c Release -o ~/voice-assistant/push-to-talk-dictation/

# Restart service
systemctl --user restart push-to-talk-dictation

# Check logs
journalctl --user -u push-to-talk-dictation -f
```

### WakeWordDetection

```bash
./deploy.sh
# Service runs on port 5000
# WebSocket: ws://localhost:5000/hubs/wakeword
```

## Models

**Whisper models location:** `~/voice-assistant/push-to-talk-dictation/models/`

| Model | Speed | Quality |
|-------|-------|---------|
| sherpa-onnx-whisper-small | ~1s/chunk | Good |
| sherpa-onnx-whisper-medium | ~5-7s/chunk | Better |

## Requirements

- .NET 10 SDK
- NVIDIA GPU with CUDA support
- ALSA audio system (Linux)

## Development

**Namespace:** `Olbrasoft.VoiceAssistant.*`

**Code Style:**
- 4-space indentation
- PascalCase for methods/classes
- `_camelCase` for private fields  
- File-scoped namespaces
- Nullable reference types enabled

## License

© 2025 Olbrasoft
