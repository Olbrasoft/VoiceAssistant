# VoiceAssistant Monorepo

Voice assistant platform containing WakeWordDetection and Orchestration projects.

## Quick Commands

**Build:** `dotnet build`  
**Test:** `dotnet test` (38 tests must pass)  
**Deploy WakeWordDetection:** `./deploy.sh`  
**Run Orchestration:** `cd src/Orchestration && dotnet run`

## Projects

### WakeWordDetection (C#/.NET 10)

Offline wake word detection service with SignalR WebSocket API.

**Build:** `dotnet build src/WakeWordDetection.Service/WakeWordDetection.Service.csproj`  
**Test:** `dotnet test tests/WakeWordDetection.Tests/ tests/WakeWordDetection.Service.Tests/`  
**Run:** `cd src/WakeWordDetection.Service && dotnet run`  
**Deploy:** `./deploy.sh` (tests → build → deploy → restart systemd)

**Port:** 5000  
**WebSocket:** `ws://localhost:5000/hubs/wakeword`  
**API:** `/api/wakeword/status`, `/api/wakeword/info`, `/api/wakeword/words`

**Wake words:** hey_jarvis, alexa, hey_mycroft, hey_rhasspy

### Orchestration (C#/.NET 10)

Voice assistant orchestrator - connects to WakeWordDetection and manages voice interactions.

**Build:** `dotnet build src/Orchestration/Orchestration.csproj`  
**Run:** `cd src/Orchestration && dotnet run`

**Phase 1 features:**
- SignalR client connection to WakeWordDetection
- Wake-word specific audio responses (Jarvis=male, Alexa=female)
- Audio playback using NAudio

**Future:** Voice recording, STT, AI integration

## Code Style

**Common for all projects:**
- 4-space indent
- PascalCase methods/classes
- `_camelCase` private fields
- File-scoped namespaces
- Nullable enabled
- XML docs for public APIs

## Testing

**Framework:** xUnit + Moq  
**Pattern:** Arrange-Act-Assert  
**Naming:** `MethodName_Scenario_ExpectedResult`

**MANDATORY before commits:** Run `dotnet test` - all 38 tests must pass.

## Architecture

**Namespace:** `Olbrasoft.VoiceAssistant.*`

**Dependencies:**
- WakeWordDetection (core library) ← no dependencies
- WakeWordDetection.Service ← depends on WakeWordDetection
- Orchestration ← SignalR client, NAudio

**Dependency Injection:** Constructor injection, interfaces for services

## Audio Assets

Location: `assets/audio/`
- `ano.mp3` - Male Czech voice (for Jarvis)
- `yes.mp3` - Female English voice (for Alexa)

Generated with: `edge-tts --voice <voice> --text "<text>" --write-media <file>.mp3`
