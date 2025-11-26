# VoiceAssistant

Voice assistant platform with wake word detection and orchestration for voice-controlled interactions.

## Projects

### 1. WakeWordDetection
Offline wake word detection service using OpenWakeWord and ONNX models.

**Technology:** .NET 10, ASP.NET Core, SignalR WebSocket, ALSA audio

**Features:**
- Offline wake word detection (hey_jarvis, alexa, hey_mycroft, hey_rhasspy)
- Per-model threshold configuration
- Real-time WebSocket notifications via SignalR
- REST API for status and testing

**Location:** `src/WakeWordDetection/`, `src/WakeWordDetection.Service/`

### 2. Orchestration
Voice assistant orchestrator that coordinates wake word detection, audio responses, and voice processing.

**Technology:** .NET 10 Worker Service, SignalR Client, NAudio

**Features (Phase 1):**
- Connects to WakeWordDetection via SignalR WebSocket
- Plays wake-word specific audio responses:
  - `hey_jarvis` → Male voice ("Ano" - Czech)
  - `alexa` → Female voice ("Yes" - English)

**Future phases:** Voice recording, STT integration, AI command processing

**Location:** `src/Orchestration/`

## Structure

```
VoiceAssistant/
├── src/
│   ├── WakeWordDetection/              # Core wake word detection library
│   ├── WakeWordDetection.Service/      # ASP.NET Core service with SignalR
│   └── Orchestration/                   # Voice assistant orchestrator
├── tests/
│   ├── WakeWordDetection.Tests/        # Core library tests (5 tests)
│   └── WakeWordDetection.Service.Tests/# Service tests (33 tests)
├── assets/
│   └── audio/                           # Audio response files
│       ├── ano.mp3                      # Male Czech voice
│       └── yes.mp3                      # Female English voice
├── VoiceAssistant.sln                   # Solution file
└── deploy.sh                            # Deployment script
```

## Build & Test

```bash
# Build entire solution
dotnet build

# Run all tests (38 tests)
dotnet test

# Run specific project
cd src/WakeWordDetection.Service && dotnet run
cd src/Orchestration && dotnet run
```

## Deployment

```bash
# Deploy WakeWordDetection service
./deploy.sh

# Service runs on port 5000
# WebSocket: ws://localhost:5000/hubs/wakeword
```

## Development

**Namespace:** `Olbrasoft.VoiceAssistant.*`

**Code Style:**
- 4-space indentation
- PascalCase for methods/classes
- `_camelCase` for private fields  
- File-scoped namespaces
- Nullable reference types enabled
- XML documentation for public APIs

**Testing:** xUnit + Moq, Arrange-Act-Assert pattern

## Requirements

- .NET 10 SDK
- ALSA audio system (Linux)
- Audio output device for orchestration

## Workflow

```
User says "Hey Jarvis"
    ↓
WakeWordDetection (port 5000) detects wake word
    ↓
SignalR broadcasts WakeWordDetected event
    ↓
Orchestration receives event
    ↓
Plays "Ano" (male voice)
    ↓
[Future: Records voice → STT → AI processing]
```

## License

© 2025 Olbrasoft
