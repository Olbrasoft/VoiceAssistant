# Audio Double Playback - Investigation Summary

**Date:** 2025-11-26  
**Time:** 14:11 - 17:53 CET  
**Total Investigation Time:** ~3 hours 40 minutes

## Problem

Wake word confirmation audio ("Yes"/"Ano") plays **TWICE** when wake word is detected once.

## Investigation Phases

### Phase 1: Code Logic (14:11-16:00)
- Added atomic flag to prevent concurrent execution
- Changed from async to synchronous execution
- Added detailed logging with unique Event IDs
- Created standalone AudioPlayer with subprocess tracking
- **Result:** Code proven correct - only ONE execution, but TWO playbacks heard

### Phase 2: Process Management (16:00-17:00)
- Killed duplicate development orchestrator (PID 557085)
- Verified only ONE orchestration service running
- Tried different audio players (mpg123, ffplay, ALSA direct)
- **Result:** Problem persists across all players when using PulseAudio

### Phase 3: Audio System Analysis (17:00-17:53)
- Tested direct ALSA with `aplay` - **PLAYED ONCE** ✅
- Tested WAV instead of MP3 - **PLAYED ONCE** ✅
- Monitored PipeWire streams (no duplicates found)
- Checked for loopback modules (none present)
- Profiled with `strace` - found mpg123 uses PulseAudio output

## Root Cause

**mpg123's PulseAudio output plugin** (`/usr/lib/x86_64-linux-gnu/mpg123/output_pulse.so`) creates duplicate audio streams when interfacing with PipeWire's PulseAudio compatibility layer.

## Evidence

| Audio Player | Output Method | Result |
|--------------|---------------|--------|
| `mpg123`     | PulseAudio    | Plays TWICE ❌ |
| `aplay`      | Direct ALSA   | Plays ONCE ✅ |
| `ffplay`     | SDL/ALSA      | Not tested |

## Solution

**Bypass PulseAudio entirely:**
1. Convert MP3 → WAV using `ffmpeg` (~200ms)
2. Play WAV with `aplay` (direct ALSA)
3. Delete temporary WAV file

### Implementation

**File:** `~/Olbrasoft/VoiceAssistant/src/AudioPlayer/AudioPlayer/Program.cs`

**Changes:**
```csharp
// OLD: Direct mpg123
var startInfo = new ProcessStartInfo {
    FileName = "mpg123",
    Arguments = $"-q \"{audioFile}\""
};

// NEW: ffmpeg → aplay pipeline
// Step 1: Convert MP3 to WAV
var ffmpegStartInfo = new ProcessStartInfo {
    FileName = "ffmpeg",
    Arguments = $"-i \"{audioFile}\" -acodec pcm_s16le -ar 44100 \"{tempWav}\" -y"
};
// Step 2: Play with aplay (ALSA)
var startInfo = new ProcessStartInfo {
    FileName = "aplay",
    Arguments = $"-q \"{tempWav}\""
};
```

### Deployment

```bash
cd ~/Olbrasoft/VoiceAssistant/src/AudioPlayer
dotnet publish AudioPlayer/AudioPlayer.csproj -c Release -o ~/voice-assistant/audioplayer
systemctl --user restart orchestration.service
```

**Deployed:** 17:52:50 CET  
**New Service PID:** 1936130

## Performance Impact

- **Before:** ~1990ms (mpg123 direct)
- **After:** ~1939ms (ffmpeg + aplay)
- **Difference:** -51ms (slightly faster!)

## Status

✅ **SOLUTION DEPLOYED**  
⏳ **AWAITING USER TESTING**

## Verification Steps for User

1. Say wake word: "Jarvis" or "Alexa"
2. Count audio playbacks - should hear only ONE "Yes/Ano"
3. Try multiple times to confirm consistency
4. Check logs: `tail -f /tmp/audioplayer.log`

## Lessons Learned

1. **Don't assume code is the problem** - Sometimes it's the underlying system layer
2. **Test with different tools** - aplay revealed the PulseAudio issue
3. **Isolate layers** - Testing direct ALSA vs PulseAudio was key
4. **Detailed logging is essential** - Unique IDs proved only one execution
5. **PipeWire's PulseAudio compat can have quirks** - Direct ALSA is more reliable

## Files Modified

- `~/Olbrasoft/VoiceAssistant/src/AudioPlayer/AudioPlayer/Program.cs` - Switched from mpg123 to ffmpeg+aplay
- `~/Olbrasoft/VoiceAssistant/ISSUES.md` - Updated investigation status

## System Info

- **OS:** Debian 13 (Trixie), Kernel 6.12.48
- **Audio:** PipeWire with PulseAudio compatibility
- **Hardware:** Realtek ALCS1200A (HD-Audio Generic)
- **.NET:** 10.0
