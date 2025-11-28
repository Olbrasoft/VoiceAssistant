# PushToTalkDictation.Service

Background service that runs the Push-to-Talk dictation system with SignalR notifications.

## Features

- Runs as a .NET Worker Service with WebHost
- Monitors CapsLock key for push-to-talk activation
- Records audio while key is held
- Transcribes speech using Whisper with GPU acceleration
- Types transcribed text into the active application
- **SignalR endpoint** for real-time event notifications

## SignalR Endpoint

The service exposes a SignalR hub at `http://localhost:5050/hubs/ptt` for real-time notifications.

### Events

| Event | Description | Data |
|-------|-------------|------|
| `Connected` | Client connected to hub | `connectionId` |
| `Subscribed` | Client subscribed successfully | `clientName` |
| `PttEvent` | PTT state change notification | `PttEvent` object |

### PttEvent Types

| EventType | Value | Description |
|-----------|-------|-------------|
| `RecordingStarted` | 0 | Recording has started |
| `RecordingStopped` | 1 | Recording has stopped (includes `durationSeconds`) |
| `TranscriptionStarted` | 2 | Transcription process started |
| `TranscriptionCompleted` | 3 | Transcription successful (includes `text`, `confidence`) |
| `TranscriptionFailed` | 4 | Transcription failed (includes `errorMessage`) |

### Example PttEvent JSON

```json
{
  "eventType": 3,
  "timestamp": "2025-01-28T12:00:00Z",
  "text": "Hello world",
  "confidence": 0.95,
  "durationSeconds": 2.5,
  "errorMessage": null,
  "serviceVersion": "1.0.0"
}
```

## Running

### Manual Start
```bash
cd ~/voice-assistant/ptt-dictation
~/.dotnet/dotnet PushToTalkDictation.Service.dll
```

### As Background Process
```bash
cd ~/voice-assistant/ptt-dictation
setsid ~/.dotnet/dotnet PushToTalkDictation.Service.dll >> /tmp/ptt-dictation.log 2>&1 &
```

## Testing the SignalR Endpoint

### Using Python Client
```bash
pip install signalrcore
cd tools
python3 test-ptt-client.py
```

### Using websocat
```bash
cd tools
./test-ptt-websocat.sh
```

### Using curl (health check)
```bash
curl http://localhost:5050/
# Response: {"service":"PushToTalkDictation","status":"running"}
```

## Configuration

The service uses `appsettings.json` for configuration:
- Audio device settings
- Whisper model path
- Keyboard device path

## Requirements

- Linux with evdev support
- ALSA audio system
- CUDA-capable GPU (for fast transcription)
- dotool installed (for Wayland text input)
- User in `input` group (for keyboard access)

## Dependencies

- **PushToTalkDictation** - Core library
- **Microsoft.AspNetCore.SignalR** - Real-time notifications

## Transcription Indicator (System Tray)

A companion Python script displays an animated icon in the system tray during transcription.

### How it works

1. Connects to SignalR hub via WebSocket
2. Shows animated document icon when `RecordingStopped` event received
3. Hides icon when `TranscriptionCompleted` or `TranscriptionFailed` received
4. Animation cycles through 5 SVG frames at 200ms intervals

### Files

| File | Description |
|------|-------------|
| `transcription-indicator.py` | Python GTK script with AppIndicator |
| `transcription-indicator.service` | Systemd user service unit |
| `assets/document-white-frame*.svg` | Animation frames (1-5) |

### Installation

The deploy script installs both services:

```bash
./deploy-push-to-talk-dictation.sh
```

### Manual Start

```bash
# Activate Python venv
source ~/voice-assistant/push-to-talk-dictation/venv/bin/activate

# Run indicator
python3 transcription-indicator.py
```

### Requirements

- Python 3 with `websocket-client`, `requests`
- GTK 3 with AyatanaAppIndicator3
- GNOME Shell with AppIndicator extension
