# Known Issues

## 🔴 CRITICAL: Double Audio Playback on Wake Word Detection

**Status:** INVESTIGATING  
**Priority:** HIGH  
**Date Reported:** 2025-11-26  
**Affected Component:** Orchestration Service, AudioPlayer

### Problem Description

When wake word ("Alexa" or "Jarvis") is detected, the confirmation audio ("Yes"/"Ano") plays **TWICE** instead of once, even when the wake word is spoken only once.

### Symptoms

- User says wake word **ONCE**
- Audio response plays **TWICE** in quick succession
- Problem is consistent and reproducible
- Occurs on first wake word after idle AND on rapid successive wake words

### What We've Ruled Out

✅ **NOT the issue:**
- Audio files are correct (only contain single "Yes" - verified with ffmpeg analysis)
- Code logic is correct (atomic flag prevents concurrent execution)
- Only ONE SignalR event received per wake word (verified in logs)
- Only ONE AudioPlayer process spawned per event (verified with unique Invocation IDs)
- Only ONE orchestration service running (killed duplicate development process)
- Async/await issues (converted to synchronous `WaitForExit()` - no change)
- mpg123 itself (logs show only one execution per trigger)

### Investigation Evidence

**AudioPlayer Log Example (Single Wake Word):**
```
========================================
[085ff15e] STARTED
[085ff15e] PID: 1784208
[085ff15e] Timestamp: 2025-11-26 17:29:00.443
[085ff15e] Audio file: .../assets/audio/yes.mp3
[085ff15e] Starting mpg123...
[085ff15e] mpg123 PID: 1784222
[085ff15e] mpg123 exited with code: 0
[085ff15e] Duration: 1990ms
[085ff15e] COMPLETED
========================================
```

**Only ONE invocation**, but user hears TWO playbacks.

**Orchestration Log:**
```
🔔 [Event a1e41e36] Wake word detected: alexa_v0.1_t0.7
🔊 [Event a1e41e36] Playing: yes.mp3
🎵 [Process 97d123] PID=1784208, waiting for exit...
🎵 [Process 97d123] Exited with code 0
✅ [Event a1e41e36] Completed
🔓 [Event a1e41e36] Released
```

**Only ONE event processed**, atomic flag working correctly.

### Possible Remaining Causes

1. **Hardware Audio Loopback**
   - Built-in motherboard audio routing might duplicate output
   - Monitor ports in PipeWire could be creating echo

2. **PipeWire Stream Duplication**
   - PipeWire might be creating two streams for single audio output
   - Cold start initialization issue

3. **ALSA Driver Bug**
   - Audio driver might be duplicating PCM output
   - Specific to this hardware/chipset (Realtek ALCS1200A)

4. **Process Fork Issue**
   - External AudioPlayer process might be forking unexpectedly
   - Shell wrapper script execution oddity

5. **Hidden Second Process**
   - Some background process catching audio and replaying it
   - Monitoring/recording software interfering

### System Information

- **OS:** Debian GNU/Linux 13 (Trixie)
- **Kernel:** 6.12.48+deb13-amd64
- **Audio System:** PipeWire
- **Audio Card:** HD-Audio Generic (Realtek ALCS1200A)
- **Microphone:** TONOR TC30 USB Audio Device
- **.NET Version:** 10.0

### Code Changes Attempted

1. ✅ Added atomic flag (`Interlocked.CompareExchange`) to prevent concurrent execution
2. ✅ Changed from `async/await` to synchronous blocking calls
3. ✅ Changed from `WaitForExitAsync()` to `WaitForExit()`
4. ✅ Added 100ms delay before playback (no effect)
5. ✅ Tried ALSA direct output bypassing PipeWire (no effect)
6. ✅ Created standalone AudioPlayer project with detailed logging

### Investigation Performed (2025-11-26 17:50-17:53)

✅ **Completed Tests:**
- [x] Tested with `aplay` (direct ALSA) - **PLAYED ONLY ONCE** ✅
- [x] Tested with WAV file instead of MP3 - **PLAYED ONLY ONCE** ✅
- [x] Monitored PipeWire streams during playback (no duplicate streams found)
- [x] Checked for audio loopback modules - **NONE FOUND** ✅
- [x] Profiled with `strace` to analyze system calls
- [x] Identified that `mpg123` uses **PulseAudio output plugin** (not direct ALSA)

### Root Cause Identified

The issue is related to **mpg123 using PulseAudio/PipeWire compatibility layer**, which may be creating duplicate audio streams or buffering issues.

**Evidence:**
- `aplay` (direct ALSA) = plays ONCE ✅
- `mpg123` (via PulseAudio) = plays TWICE ❌

### Solution Applied

**Modified AudioPlayer to bypass PulseAudio:**
1. Convert MP3 to WAV using `ffmpeg`
2. Play WAV directly with `aplay` (ALSA)
3. Clean up temporary WAV file

**File Modified:** `/home/jirka/Olbrasoft/VoiceAssistant/src/AudioPlayer/AudioPlayer/Program.cs`

**Deployment:**
- Rebuilt AudioPlayer: `dotnet publish -c Release`
- Deployed to: `~/voice-assistant/audioplayer/`
- Restarted service: `systemctl --user restart orchestration.service`
- New PID: 1936130

### Status

**TESTING IN PROGRESS** - Updated deployment at 19:48:00 CET

Changes deployed:
1. AudioPlayer: ffmpeg + aplay (bypasses PulseAudio)
2. Orchestrator: Refactored to new class with speech recognition integration
3. All services restarted

**User reports issue still persists** - investigating further.

Possible causes still being explored:
- Audio system configuration (PipeWire/ALSA routing)
- Hardware-level audio duplication

### Related Files

- `/home/jirka/Olbrasoft/VoiceAssistant/src/Orchestration/Orchestrator.cs`
- `/home/jirka/Olbrasoft/VoiceAssistant/src/Orchestration/Services/AudioResponsePlayer.cs`
- `/home/jirka/Olbrasoft/VoiceAssistant/src/AudioPlayer/AudioPlayer/Program.cs`
- `/home/jirka/voice-assistant/audioplayer/play.sh`

### Logs

- **AudioPlayer Log:** `/tmp/audioplayer.log`
- **Orchestration Service:** `journalctl --user -u orchestration.service`
- **WakeWord Listener:** `journalctl --user -u wakeword-listener.service`

---

## ⚠️ MEDIUM: Speech-to-Text Not Inserting Text

**Status:** INVESTIGATING  
**Priority:** MEDIUM  
**Date Reported:** 2025-11-26  
**Affected Component:** Speech Recognition, CapsLock Dictation

### Problem Description

When using CapsLock dictation (CapsLock ON → speak → CapsLock OFF), the audio is recorded and transcribed by Whisper, but the recognized text is not being inserted into the active window.

### Symptoms

- CapsLock ON starts recording ✅
- Audio is captured successfully ✅
- Whisper model loads and processes audio ✅
- Sometimes Whisper returns empty string ("⚠️ Žádný text nebyl rozpoznán")
- Text insertion via xdotool fails or doesn't occur

### Investigation Needed

- Check if xdotool is working correctly
- Verify PyAudio is capturing from correct microphone
- Check if microphone is in SUSPENDED state during capture
- Verify Whisper model performance and accuracy

### Related Files

- `/home/jirka/voice-assistant/push-to-talk-dictation/speech-to-text-simple.py`
- `/home/jirka/voice-assistant/push-to-talk-dictation/speech-to-text-capslock-monitor-v2.sh`

---

**Last Updated:** 2025-11-26 17:53 - Solution deployed, awaiting user testing
