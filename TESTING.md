# Speech-to-Text Dictation Testing Guide

## Overview
The VoiceAssistant now supports **speech-to-text dictation** workflow:
1. **Wake word detection** (Hey Jarvis / Alexa)
2. **Audio confirmation** ("Ano" / "Yes")
3. **Audio recording** with silence detection
4. **Speech transcription** using faster-whisper (GPU)
5. **Text typing** into focused window using xdotool

## Prerequisites

### 1. Check Python Dependencies
```bash
pip3 list | grep -E "faster-whisper|nvidia"
```

Should show:
- `faster-whisper` (with CUDA support)
- `nvidia-cudnn-cu12`, `nvidia-cublas-cu12`, `nvidia-cuda-runtime-cu12`

### 2. Verify Transcription Script
```bash
python3 /home/jirka/Olbrasoft/VoiceAssistant/scripts/transcribe-audio.py
```

Should output: `{"error": "Usage: transcribe-audio.py <audio_file.wav>"}`

### 3. Check xdotool
```bash
which xdotool
```

Should output: `/usr/bin/xdotool`

## Testing Workflow

### Step 1: Start WakeWord Listener Service
```bash
# Check if already running
systemctl --user status wakeword-listener.service

# If not running:
systemctl --user start wakeword-listener.service

# Monitor logs:
journalctl --user -u wakeword-listener.service -f
```

### Step 2: Start Orchestration Service
```bash
cd /home/jirka/Olbrasoft/VoiceAssistant/src/Orchestration

# Run in foreground (for debugging):
dotnet run

# Expected output:
# info: Olbrasoft.VoiceAssistant.Orchestration.Worker[0]
#       Worker started at: ...
# info: Olbrasoft.VoiceAssistant.Orchestration.Orchestrator[0]
#       Connecting to WakeWordDetection service at http://localhost:5000/hubs/wakeword
# info: Olbrasoft.VoiceAssistant.Orchestration.Orchestrator[0]
#       Connected to WakeWordDetection service
```

### Step 3: Prepare Test Window
```bash
# Open a text editor or terminal where you want text to appear:
gedit test.txt
# or
xed test.txt
# or focus on any text input field
```

### Step 4: Test Speech-to-Text

1. **Say wake word**: "Hey Jarvis" or "Alexa"
   - ✅ Expected: You should hear "Ano" (male) or "Yes" (female)
   - 🎤 Recording starts automatically

2. **Speak your text**: Say something in Czech (e.g., "Ahoj, jak se máš?")
   - Wait 3 seconds after speaking (silence detection)
   - ⏱️ Recording stops automatically

3. **Wait for transcription**: ~2-5 seconds (depends on GPU)
   - 📝 Logs will show: "Transcribed: ..."

4. **Text appears**: The transcribed text should be typed into your focused window
   - ⌨️ Logs will show: "Typing text: ..."
   - ✅ Logs will show: "Speech-to-text workflow completed successfully"

## Troubleshooting

### No audio confirmation heard
- Check audio files exist: `ls -la ~/cml/voice-output/cache/`
- Should contain: `ano-cml.mp3`, `yes.mp3`
- Check `AudioResponsePlayer` logs

### Recording fails
- Check microphone permissions
- Verify NAudio can access audio device:
  ```bash
  pactl list sources short
  ```

### Transcription returns empty
- Test transcription script manually:
  ```bash
  # Record a test file:
  arecord -d 3 -f S16_LE -r 16000 -c 1 test.wav
  
  # Transcribe it:
  python3 scripts/transcribe-audio.py test.wav
  ```
- Check GPU availability: `nvidia-smi`
- Verify faster-whisper model downloaded: `~/.cache/huggingface/hub/`

### Text not typed
- Verify xdotool works:
  ```bash
  xdotool type "Test text"
  ```
- Check window focus (xdotool requires X11)

## Expected Log Output (Success)

```
info: Orchestrator[0]
      🎤 Starting speech recognition...
info: SpeechRecognitionService[0]
      🎤 Recording audio to /tmp/voice-assistant/recording_20251126_154723.wav...
info: SpeechRecognitionService[0]
      📊 Calibrated silence threshold: 850
info: SpeechRecognitionService[0]
      ✅ Audio recorded: 2.3s, 73600 bytes
info: SpeechRecognitionService[0]
      🔄 Transcribing audio: /tmp/voice-assistant/recording_20251126_154723.wav
info: SpeechRecognitionService[0]
      📝 Transcribed: Ahoj, jak se máš?
info: Orchestrator[0]
      ⌨️  Typing transcribed text...
info: TextInputService[0]
      ⌨️  Typing text: Ahoj, jak se máš?
info: TextInputService[0]
      ✅ Text typed successfully
info: Orchestrator[0]
      ✅ Speech-to-text workflow completed successfully
```

## Running Tests

```bash
# Run all tests:
cd /home/jirka/Olbrasoft/VoiceAssistant
dotnet test

# Run only Orchestration tests:
dotnet test tests/Orchestration.Tests/Orchestration.Tests.csproj

# Expected:
# ✅ 53 tests total
# - 37 WakeWordDetection tests
# - 16 Orchestration tests (including SpeechRecognitionService + TextInputService)
```

## Known Limitations

1. **Audio recording** requires microphone access (won't work in Docker without device mapping)
2. **xdotool** requires X11 (won't work in Wayland without XWayland)
3. **GPU transcription** requires NVIDIA GPU with CUDA support (falls back to CPU automatically)
4. **Czech language** is default (change in `transcribe-audio.py` line 46 if needed)

## Next Steps

Once basic testing works:
1. Create systemd service for Orchestration
2. Add configuration for silence detection thresholds
3. Add support for multiple languages
4. Implement retry logic for transcription failures
