# Orchestration

Central orchestration service that coordinates all Voice Assistant components.

## Features

- **Service Coordination** - Manages WakeWord, STT, and TTS services
- **SignalR Communication** - Real-time communication between components
- **Audio Processing** - Handles audio routing between services
- **Command Processing** - Processes voice commands after wake word detection

## Components

- **IOrchestrator** - Interface for orchestration logic
- **Orchestrator** - Main orchestration implementation
- **Worker** - Background worker service

## Workflow

1. Listens for wake word detection from WakeWordDetection.Service
2. Activates audio recording
3. Sends audio to speech recognition
4. Processes transcribed commands
5. Generates responses via TTS

## Dependencies

- **Microsoft.AspNetCore.SignalR.Client** - SignalR client
- **Whisper.net** - Speech recognition
- **OpenTK.Audio.OpenAL** - Audio I/O
- **Microsoft.Extensions.Hosting** - Worker service

## Configuration

Configure in `appsettings.json`:
- Service endpoints (WakeWord, TTS)
- Audio devices
- Whisper model path

## Running

```bash
cd src/Orchestration
dotnet run
```

## Status

Currently in development. The Push-to-Talk dictation system is the primary focus.
