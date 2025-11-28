# PushToTalkDictation

Core library for Push-to-Talk dictation functionality on Linux.

## Features

- **Keyboard Monitoring** - Detects CapsLock key press/release using evdev
- **Audio Recording** - Records audio using ALSA while key is held
- **Speech Recognition** - Transcribes audio using Whisper with GPU acceleration
- **Text Injection** - Types transcribed text into the active application

## Components

### Keyboard
- **IKeyboardMonitor** - Interface for keyboard event monitoring
- **EvdevKeyboardMonitor** - Linux evdev implementation for keyboard monitoring

### Audio
- **IAudioRecorder** - Interface for audio recording
- **AlsaAudioRecorder** - ALSA-based audio recorder for Linux
- **AudioDataEventArgs** - Event args for audio data chunks

## Dependencies

- **NAudio** - Audio processing
- **VoiceAssistant.Shared** - Speech recognition and text input

## Workflow

1. User holds CapsLock key
2. Audio recording starts
3. User speaks
4. User releases CapsLock
5. Audio is transcribed using Whisper
6. Text is typed into the active application (Ctrl+V or Ctrl+Shift+V for terminals)

## Terminal Detection

The system automatically detects terminal applications (kitty, gnome-terminal, etc.) and uses `Ctrl+Shift+V` for pasting instead of `Ctrl+V`.
