# Push-to-Talk Dictation Service

**C# .NET implementation** of push-to-talk speech dictation using CapsLock trigger key.

## 🎯 Overview

This service monitors keyboard events (CapsLock) and triggers audio recording for speech-to-text dictation.

### Architecture

```
CapsLock Press
    ↓
EvdevKeyboardMonitor (/dev/input/eventX)
    ↓
DictationWorker
    ↓
AlsaAudioRecorder (pw-record/arecord)
    ↓
[TODO] SpeechTranscriber (Whisper)
    ↓
[TODO] XdotoolTextTyper
```

## 📁 Project Structure

```
src/PushToTalkDictation/               # Core library
├── IKeyboardMonitor.cs                # Keyboard monitoring interface
├── EvdevKeyboardMonitor.cs            # Linux evdev implementation
├── IAudioRecorder.cs                  # Audio recording interface
├── AlsaAudioRecorder.cs               # Linux ALSA/PipeWire implementation
├── NAudioRecorder.cs                  # Windows NAudio implementation
├── ITextTyper.cs                      # Text typing interface
├── XdotoolTextTyper.cs                # Linux xdotool implementation
└── KeyCode.cs, KeyEventArgs.cs        # Event models

src/PushToTalkDictation.Service/       # Background service
├── DictationWorker.cs                 # Main worker service
├── Program.cs                         # Service host
└── appsettings.json                   # Configuration

tests/PushToTalkDictation.Tests/       # Unit tests (35 tests)
```

## 🚀 Installation

### 1. Prerequisites

**Add user to input group** (required for /dev/input access):

```bash
sudo usermod -a -G input $USER
```

**IMPORTANT:** Logout and login again for group changes to take effect!

Verify:
```bash
groups | grep input
```

### 2. Deploy Service

```bash
cd ~/Olbrasoft/VoiceAssistant
./deploy-push-to-talk-dictation.sh
```

The deployment script will:
1. ✅ Run all tests
2. 📦 Build in Release mode
3. 🚀 Deploy to `~/voice-assistant/push-to-talk-dictation/`
4. ⚙️  Install systemd service
5. 🔄 Restart service

### 3. Enable on Boot (Optional)

```bash
systemctl --user enable push-to-talk-dictation.service
```

## 🎮 Usage

### Start/Stop Service

```bash
# Start
systemctl --user start push-to-talk-dictation.service

# Stop
systemctl --user stop push-to-talk-dictation.service

# Status
systemctl --user status push-to-talk-dictation.service

# View logs
journalctl --user -u push-to-talk-dictation.service -f
```

### Using Dictation

1. **Press and hold CapsLock** → Recording starts
2. **Speak your text**
3. **Release CapsLock** → Recording stops, transcription begins

**Grace Period:** First 3 seconds ignored (prevents accidental stops)

## ⚙️ Configuration

Edit `~/voice-assistant/push-to-talk-dictation/appsettings.json`:

```json
{
  "PushToTalkDictation": {
    "KeyboardDevice": null,          // Auto-detect keyboard
    "TriggerKey": "CapsLock",        // Trigger key (CapsLock, ScrollLock, etc.)
    "GracePeriodSeconds": 3.0,       // Minimum recording duration
    "AudioSampleRate": 16000,        // Audio sample rate (Hz)
    "AudioChannels": 1,              // Mono
    "AudioBitsPerSample": 16         // 16-bit PCM
  }
}
```

## 🧪 Testing

```bash
cd ~/Olbrasoft/VoiceAssistant

# Run all tests
dotnet test

# Run specific test project
dotnet test tests/PushToTalkDictation.Tests/

# Verbose output
dotnet test --verbosity normal
```

**Test Results:** 35/35 tests passing ✅

## 🔧 Troubleshooting

### Permission Denied Error

```
UnauthorizedAccessException: Permission denied accessing /dev/input/eventX
```

**Solution:**
```bash
sudo usermod -a -G input $USER
# Logout and login!
```

### Keyboard Not Detected

Check available input devices:
```bash
ls -la /dev/input/by-path/ | grep kbd
```

Manually specify device in `appsettings.json`:
```json
"KeyboardDevice": "/dev/input/event3"
```

### Service Won't Start

Check logs:
```bash
journalctl --user -u push-to-talk-dictation.service -n 50
```

Verify deployment:
```bash
ls -la ~/voice-assistant/push-to-talk-dictation/
```

## 🌍 Cross-Platform Support

| Component | Linux | Windows | Notes |
|-----------|-------|---------|-------|
| **Keyboard Monitor** | ✅ evdev | ⏳ Planned | Windows: Use SetWindowsHookEx |
| **Audio Recorder** | ✅ ALSA/PipeWire | ✅ NAudio | Both implementations included |
| **Text Typer** | ✅ xdotool | ⏳ Planned | Windows: Use SendInput API |

## 📊 Dependencies

- **.NET 10.0**
- **Microsoft.Extensions.Hosting** (9.0.0)
- **NAudio** (2.2.1) - Windows audio support
- **xUnit + Moq** - Unit testing

**System Dependencies (Linux):**
- `pw-record` or `arecord` - Audio recording
- `xdotool` - Text typing
- `/dev/input/eventX` access - Keyboard monitoring

## 🔮 TODO

- [ ] Implement ISpeechTranscriber (Whisper integration)
- [ ] Implement IPushToTalkDictator (orchestrator)
- [ ] Add Windows keyboard monitoring support
- [ ] Add Windows text typing support
- [ ] Add configuration UI
- [ ] Add audio visualization during recording
- [ ] Add voice activity detection (VAD)

## 📝 Development Workflow

```bash
# 1. Make changes in src/PushToTalkDictation/

# 2. Run tests
dotnet test

# 3. Deploy (if tests pass)
./deploy-push-to-talk-dictation.sh

# 4. Monitor logs
journalctl --user -u push-to-talk-dictation.service -f
```

## 🎓 Architecture Patterns

- ✅ **SOLID Principles**
- ✅ **Dependency Injection** (via Microsoft.Extensions.DI)
- ✅ **Interface Segregation** (small, focused interfaces)
- ✅ **Cross-platform abstraction** (evdev vs NAudio)
- ✅ **Background Services** (IHostedService pattern)

---

**Generated by Claude Code** 🤖
