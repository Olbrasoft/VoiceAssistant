# VoiceAssistant

Voice assistant platform for Linux with push-to-talk dictation, continuous listening, and text-to-speech.

## Features

- **Push-to-Talk Dictation** - Hold CapsLock to record, release to transcribe using GPU-accelerated Whisper
- **Continuous Listening** - Background wake word detection using Whisper transcription
- **Text-to-Speech** - Microsoft Edge TTS integration via WebSocket with stop functionality
- **Transcription History** - SQLite database logging of all transcriptions

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        VoiceAssistant Platform                       │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌──────────────────┐    ┌──────────────────┐    ┌───────────────┐  │
│  │  PushToTalk      │    │  Continuous      │    │  EdgeTts      │  │
│  │  Dictation       │    │  Listener        │    │  WebSocket    │  │
│  │  Service :5050   │    │  (VAD+Whisper)   │    │  Server :5555 │  │
│  └────────┬─────────┘    └────────┬─────────┘    └───────┬───────┘  │
│           │                       │                      │          │
│           ▼                       ▼                      ▼          │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │                    VoiceAssistant.Shared                     │   │
│  │  (Whisper transcription, text input, audio processing)       │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │              VoiceAssistant.Data.EntityFrameworkCore         │   │
│  │  (SQLite database for transcription history)                  │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

## Projects

| Project | Description | Type |
|---------|-------------|------|
| [VoiceAssistant.Shared](src/VoiceAssistant.Shared/) | Shared library with speech recognition, text input, audio processing | Library |
| [VoiceAssistant.Data.EntityFrameworkCore](src/VoiceAssistant.Data.EntityFrameworkCore/) | EF Core data layer with SQLite for transcription history | Library |
| [PushToTalkDictation](src/PushToTalkDictation/) | Core library for push-to-talk recording and keyboard monitoring | Library |
| [PushToTalkDictation.Service](src/PushToTalkDictation.Service/) | Systemd service for CapsLock-triggered dictation (port 5050) | Worker Service |
| [ContinuousListener](src/ContinuousListener/) | Background listener with VAD + Whisper wake word detection | Worker Service |
| [EdgeTtsWebSocketServer](src/EdgeTtsWebSocketServer/) | Text-to-Speech server using Microsoft Edge TTS (port 5555) | Web API |

## Quick Start

### Prerequisites

- .NET 10 SDK
- Linux with PipeWire audio system
- NVIDIA GPU with CUDA (for Whisper transcription)
- User in `input` group (for keyboard access)

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
# Edge TTS Server (port 5555)
dotnet publish src/EdgeTtsWebSocketServer -c Release -o ~/voice-assistant/edge-tts-server
systemctl --user start edge-tts-server

# Push-to-Talk Dictation (port 5050)
dotnet publish src/PushToTalkDictation.Service -c Release -o ~/voice-assistant/push-to-talk-dictation
systemctl --user start push-to-talk-dictation
```

## Services

### Push-to-Talk Dictation (Port 5050)

Hold CapsLock to record audio, release to transcribe and type text.

Features:
- Automatic TTS interruption when recording starts
- Transcription history saved to SQLite database
- SignalR WebSocket notifications for UI indicators

```bash
# Check status
systemctl --user status push-to-talk-dictation

# View logs
journalctl --user -u push-to-talk-dictation -f

# SignalR endpoint
ws://localhost:5050/hubs/dictation
```

### Edge TTS Server (Port 5555)

Text-to-Speech using Microsoft Edge TTS with caching.

```bash
# Speak text
curl -X POST http://localhost:5555/api/speech/speak \
  -H "Content-Type: application/json" \
  -d '{"text":"Ahoj světe"}'

# Stop current playback
curl -X POST http://localhost:5555/api/speech/stop

# Clear cache
curl -X DELETE http://localhost:5555/api/speech/cache
```

### Continuous Listener

Background service that continuously listens for speech using VAD (Voice Activity Detection) and transcribes with Whisper to detect wake words.

## Technology Stack

- **.NET 10** - Runtime and SDK
- **ASP.NET Core** - Web API and SignalR
- **Entity Framework Core** - SQLite database access
- **Whisper.net** - Speech-to-text with CUDA GPU acceleration
- **PipeWire** - Audio capture on Linux
- **Microsoft Edge TTS** - Text-to-speech via WebSocket

## Database

Transcription history is stored in SQLite database at `~/voice-assistant/voice-assistant.db`.

```sql
-- TranscriptionLogs table
CREATE TABLE TranscriptionLogs (
    Id INTEGER PRIMARY KEY,
    Text TEXT NOT NULL,
    Confidence REAL NOT NULL,
    DurationMs INTEGER NOT NULL,
    Source INTEGER NOT NULL,  -- 0=Unknown, 1=PushToTalk, 2=ContinuousListener, etc.
    Language TEXT,
    CreatedAt TEXT NOT NULL
);
```

## Models

### Whisper Models

Located in `~/voice-assistant/push-to-talk-dictation/models/`

| Model | Speed | Quality | GPU Memory |
|-------|-------|---------|------------|
| ggml-large-v3.bin | ~2s/chunk | Best | ~3GB |

## Testing

```bash
# Run all tests (127 tests)
dotnet test

# Run specific project tests
dotnet test tests/VoiceAssistant.Shared.Tests
dotnet test tests/PushToTalkDictation.Tests
dotnet test tests/VoiceAssistant.Data.EntityFrameworkCore.Tests
```

## Project Structure

```
VoiceAssistant/
├── src/
│   ├── VoiceAssistant.Shared/              # Shared library
│   │   ├── Speech/                         # Whisper transcription
│   │   ├── TextInput/                      # Text typing (dotool)
│   │   ├── Input/                          # CapsLock state detection
│   │   └── Data/                           # Entities, commands, enums
│   ├── VoiceAssistant.Data.EntityFrameworkCore/  # EF Core + SQLite
│   ├── PushToTalkDictation/                # PTT core library
│   ├── PushToTalkDictation.Service/        # PTT systemd service
│   ├── ContinuousListener/                 # VAD + Whisper listener
│   └── EdgeTtsWebSocketServer/             # TTS server
├── tests/
│   ├── VoiceAssistant.Shared.Tests/
│   ├── VoiceAssistant.Data.EntityFrameworkCore.Tests/
│   ├── PushToTalkDictation.Tests/
│   ├── PushToTalkDictation.Service.Tests/
│   └── EdgeTtsWebSocketServer.Tests/
└── VoiceAssistant.sln
```

## Transcription Indicator

A Python systray indicator shows animated icon during speech transcription. See `transcription-indicator/` for details.

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
