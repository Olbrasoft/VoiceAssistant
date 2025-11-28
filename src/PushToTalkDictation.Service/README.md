# PushToTalkDictation.Service

Background service that runs the Push-to-Talk dictation system.

## Features

- Runs as a .NET Worker Service
- Monitors CapsLock key for push-to-talk activation
- Records audio while key is held
- Transcribes speech using Whisper with GPU acceleration
- Types transcribed text into the active application

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
- **Microsoft.Extensions.Hosting** - Worker service framework
