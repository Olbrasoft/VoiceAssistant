# VoiceAssistant

Voice assistant platform for Linux with push-to-talk dictation, wake word detection, text-to-speech, and orchestration.

## Features

- **Push-to-Talk Dictation** - Hold CapsLock to record, release to transcribe using GPU-accelerated Whisper
- **Wake Word Detection** - Offline detection using OpenWakeWord ONNX models (Jarvis, Alexa, etc.)
- **Text-to-Speech** - Microsoft Edge TTS integration via WebSocket
- **Orchestration** - Coordinates wake word detection with responses

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        VoiceAssistant Platform                       │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌──────────────────┐    ┌──────────────────┐    ┌───────────────┐  │
│  │  PushToTalk      │    │  WakeWord        │    │  EdgeTts      │  │
│  │  Dictation       │    │  Detection       │    │  WebSocket    │  │
│  │  Service         │    │  Service         │    │  Server       │  │
│  └────────┬─────────┘    └────────┬─────────┘    └───────┬───────┘  │
│           │                       │                      │          │
│           ▼                       ▼                      ▼          │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │                    VoiceAssistant.Shared                     │   │
│  │  (Whisper transcription, text input, audio processing)       │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │                      Orchestration                            │   │
│  │  (Coordinates wake word events and responses)                 │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

## Projects

| Project | Description | Type |
|---------|-------------|------|
| [VoiceAssistant.Shared](src/VoiceAssistant.Shared/) | Shared library with speech recognition, text input, audio processing | Library |
| [PushToTalkDictation](src/PushToTalkDictation/) | Core library for push-to-talk recording and keyboard monitoring | Library |
| [PushToTalkDictation.Service](src/PushToTalkDictation.Service/) | Systemd service for CapsLock-triggered dictation | Worker Service |
| [WakeWordDetection](src/WakeWordDetection/) | Core library for wake word detection using ONNX models | Library |
| [WakeWordDetection.Service](src/WakeWordDetection.Service/) | ASP.NET Core service with SignalR WebSocket notifications | Web API |
| [EdgeTtsWebSocketServer](src/EdgeTtsWebSocketServer/) | Text-to-Speech server using Microsoft Edge TTS | Web API |
| [Orchestration](src/Orchestration/) | Orchestrator connecting wake word detection with responses | Worker Service |

## Quick Start

### Prerequisites

- .NET 10 SDK
- Linux with ALSA audio system
- NVIDIA GPU with CUDA (for Whisper transcription)
- PipeWire or PulseAudio

### Build

```bash
# Clone repository
git clone https://github.com/Olbrasoft/VoiceAssistant.git
cd VoiceAssistant

# Build all projects
dotnet build

# Run tests
dotnet test
```

### Deploy Services

```bash
# Push-to-Talk Dictation
./src/PushToTalkDictation.Service/deploy.sh
systemctl --user start push-to-talk-dictation

# Wake Word Detection
./src/WakeWordDetection.Service/deploy.sh
systemctl --user start wakeword-listener

# Edge TTS Server
./src/EdgeTtsWebSocketServer/deploy-edge-tts.sh
systemctl --user start edge-tts-server
```

## Services

### Push-to-Talk Dictation (Port: N/A - keyboard triggered)

Hold CapsLock to record audio, release to transcribe to text.

```bash
# Check status
systemctl --user status push-to-talk-dictation

# View logs
journalctl --user -u push-to-talk-dictation -f
```

### Wake Word Detection (Port: 5000)

Listens for wake words and broadcasts events via SignalR.

```bash
# WebSocket endpoint
ws://localhost:5000/hubs/wakeword

# REST API
curl http://localhost:5000/swagger
```

### Edge TTS Server (Port: 5555)

Text-to-Speech using Microsoft Edge TTS.

```bash
# Speak text
curl -X POST http://localhost:5555/speak \
  -H "Content-Type: application/json" \
  -d '{"text":"Ahoj světe"}'
```

## Technology Stack

- **.NET 10** - Runtime and SDK
- **ASP.NET Core** - Web API and SignalR
- **ONNX Runtime** - ML model inference
- **Whisper.net** - Speech-to-text with CUDA GPU acceleration
- **OpenWakeWord** - Wake word detection models
- **ALSA/PipeWire** - Audio capture on Linux
- **Microsoft Edge TTS** - Text-to-speech via WebSocket

## Models

### Whisper Models

Located in `~/voice-assistant/push-to-talk-dictation/models/`

| Model | Speed | Quality | GPU Memory |
|-------|-------|---------|------------|
| ggml-large-v3.bin | ~2s/chunk | Best | ~3GB |
| sherpa-onnx-whisper-medium | ~5s/chunk | Good | ~1.5GB |
| sherpa-onnx-whisper-small | ~1s/chunk | Basic | ~500MB |

### Wake Word Models

Located in `~/voice-assistant/wake-word-detection/Models/`

- `hey_jarvis.onnx` - "Hey Jarvis" wake word
- `alexa.onnx` - "Alexa" wake word
- `hey_mycroft.onnx` - "Hey Mycroft" wake word

## Testing

```bash
# Run all tests (271 tests)
dotnet test

# Run specific project tests
dotnet test tests/VoiceAssistant.Shared.Tests
dotnet test tests/WakeWordDetection.Tests
dotnet test tests/PushToTalkDictation.Tests
```

## Project Structure

```
VoiceAssistant/
├── src/
│   ├── VoiceAssistant.Shared/          # Shared library
│   │   ├── Speech/                     # Whisper transcription
│   │   ├── TextInput/                  # Text typing (dotool, xdotool)
│   │   └── Input/                      # CapsLock state detection
│   ├── PushToTalkDictation/            # PTT core library
│   ├── PushToTalkDictation.Service/    # PTT systemd service
│   ├── WakeWordDetection/              # Wake word core library
│   ├── WakeWordDetection.Service/      # Wake word API service
│   ├── EdgeTtsWebSocketServer/         # TTS server
│   └── Orchestration/                  # Event orchestrator
├── tests/
│   ├── VoiceAssistant.Shared.Tests/    # 42 tests
│   ├── PushToTalkDictation.Tests/      # 72 tests
│   ├── PushToTalkDictation.Service.Tests/
│   ├── WakeWordDetection.Tests/        # 81 tests
│   ├── WakeWordDetection.Service.Tests/# 32 tests
│   ├── Orchestration.Tests/            # 43 tests
│   └── EdgeTtsWebSocketServer.Tests/
└── VoiceAssistant.sln
```

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
