# VoiceAssistant - Agent Instructions

**Poslední aktualizace:** 2025-11-28  
**Stav:** ✅ Plně funkční

Tento soubor obsahuje všechny informace potřebné pro práci na projektu bez nutnosti procházet kód.

---

## 🎯 O projektu

VoiceAssistant je platforma pro hlasové ovládání na Linuxu s těmito komponentami:

1. **Push-to-Talk Dictation** - Drž CapsLock, mluv, pusť → text se napíše do aktivní aplikace
2. **Wake Word Detection** - Offline detekce "Hey Jarvis" a dalších wake words
3. **Text-to-Speech** - Microsoft Edge TTS přes WebSocket
4. **Orchestration** - Koordinace wake word → odpověď

---

## 📁 Struktura projektu

```
~/Olbrasoft/VoiceAssistant/           # Git repozitář (zdrojový kód)
├── src/
│   ├── VoiceAssistant.Shared/        # Sdílená knihovna
│   │   ├── Speech/                   # OnnxWhisperTranscriber, AudioPreprocessor, TokenDecoder
│   │   ├── TextInput/                # DotoolTextTyper (Wayland text input)
│   │   └── Input/                    # CapsLockStateDetector
│   ├── PushToTalkDictation/          # Core knihovna (EvdevKeyboardMonitor, PwRecordAudioCapture)
│   ├── PushToTalkDictation.Service/  # Worker Service + SignalR hub
│   │   ├── DictationWorker.cs        # Hlavní worker
│   │   ├── PttHub.cs                 # SignalR hub na :5050/hubs/ptt
│   │   ├── PttNotifier.cs            # Broadcaster eventů
│   │   ├── transcription-indicator.py # Python systray indikátor
│   │   └── deploy-push-to-talk-dictation.sh
│   ├── WakeWordDetection/            # ONNX wake word detekce
│   ├── WakeWordDetection.Service/    # ASP.NET API + SignalR
│   ├── EdgeTtsWebSocketServer/       # TTS server
│   └── Orchestration/                # Koordinátor
├── tests/                            # 270 unit testů
└── VoiceAssistant.sln
```

**Deployment adresáře:**
```
~/voice-assistant/
├── push-to-talk-dictation/           # PTT služba
│   ├── PushToTalkDictation.Service.dll
│   ├── appsettings.json
│   ├── transcription-indicator.py
│   ├── venv/                         # Python virtualenv
│   ├── assets/                       # SVG ikony pro animaci
│   └── models/
│       └── sherpa-onnx-whisper-small/
├── wake-word-detection/              # Wake word služba
└── voice-output/                     # TTS skripty
```

---

## 🔌 Běžící služby

| Služba | Port | Endpoint | Systemd unit |
|--------|------|----------|--------------|
| Push-to-Talk Dictation | 5050 | `http://localhost:5050/hubs/ptt` | `push-to-talk-dictation.service` |
| Transcription Indicator | - | (systray) | `transcription-indicator.service` |
| Wake Word Detection | 5000 | `ws://localhost:5000/hubs/wakeword` | `wakeword-listener.service` |
| Edge TTS Server | 5555 | `http://localhost:5555/speak` | `edge-tts-server.service` |

**Kontrola služeb:**
```bash
systemctl --user status push-to-talk-dictation
systemctl --user status transcription-indicator
journalctl --user -u push-to-talk-dictation -f
```

---

## 📡 SignalR API (PushToTalkDictation)

**Hub:** `http://localhost:5050/hubs/ptt`

### PttEvent Types

| EventType | Hodnota | Popis |
|-----------|---------|-------|
| RecordingStarted | 0 | Nahrávání začalo (CapsLock stisknuto) |
| RecordingStopped | 1 | Nahrávání skončilo (obsahuje `durationSeconds`) |
| TranscriptionStarted | 2 | Přepis začal |
| TranscriptionCompleted | 3 | Přepis dokončen (obsahuje `text`, `confidence`) |
| TranscriptionFailed | 4 | Přepis selhal (obsahuje `errorMessage`) |

### Transcription Indicator

Python skript `transcription-indicator.py`:
- Připojuje se k SignalR přes raw WebSocket (ne signalrcore - ta nefungovala)
- Na `RecordingStopped` zobrazí animovanou ikonu v systray
- Na `TranscriptionCompleted/Failed` ikonu skryje
- Animace: 5 framů (`document-white-frame1-5.svg`), 200ms interval

---

## 🛠️ Vývoj a deployment

### Build & Test
```bash
cd ~/Olbrasoft/VoiceAssistant
dotnet build
dotnet test                    # 270 testů (1 přeskočen - macOS specific)
```

### Deploy Push-to-Talk Dictation
```bash
./src/PushToTalkDictation.Service/deploy-push-to-talk-dictation.sh
```

Deploy skript:
1. Zabije všechny běžící instance (prevence duplicit)
2. Spustí testy
3. Publikuje do `~/voice-assistant/push-to-talk-dictation/`
4. Aktualizuje Python venv
5. Restartuje obě systemd služby

### Ruční restart
```bash
systemctl --user restart push-to-talk-dictation
systemctl --user restart transcription-indicator
```

---

## ⚙️ Technologie

- **.NET 10** (Preview) - SDK a runtime
- **ASP.NET Core** - Web API, SignalR
- **Whisper.net** + **ONNX Runtime CUDA** - GPU-akcelerovaný přepis řeči
- **evdev** - Čtení klávesnice (CapsLock trigger)
- **pw-record** - PipeWire audio capture
- **dotool** - Wayland text input (simulace Ctrl+V)
- **GTK 3 + AyatanaAppIndicator3** - Systray ikona (Python)

---

## 📝 Code Style

- 4 mezery odsazení
- PascalCase pro metody/třídy
- `_camelCase` pro privátní fieldy
- File-scoped namespaces
- Nullable reference types enabled
- Namespace: `Olbrasoft.VoiceAssistant.*`

---

## 🐛 Známé problémy (vyřešené)

### 1. Duplicitní vkládání textu
**Příčina:** Běžely dvě instance služby  
**Řešení:** Deploy skript nyní v kroku 0 zabíjí všechny procesy

### 2. signalrcore Python knihovna nefungovala
**Příčina:** Nepřijímala eventy správně  
**Řešení:** Přepsáno na raw WebSocket s `websocket-client`

### 3. Test přehrával audio
**Příčina:** `TriggerDictationAsync` test volal skutečný kód  
**Řešení:** Test odstraněn

---

## 📋 Možná budoucí vylepšení

- [ ] Podpora více jazyků (ne jen čeština)
- [ ] Konfigurovatelná klávesa (ne jen CapsLock)
- [ ] GUI pro nastavení
- [ ] Integrace s OpenCode (HTTP API)

---

## 🔗 Klíčové soubory

| Soubor | Účel |
|--------|------|
| `src/PushToTalkDictation.Service/DictationWorker.cs` | Hlavní worker - nahrávání a přepis |
| `src/PushToTalkDictation.Service/PttHub.cs` | SignalR hub |
| `src/PushToTalkDictation.Service/transcription-indicator.py` | Systray indikátor |
| `src/VoiceAssistant.Shared/Speech/OnnxWhisperTranscriber.cs` | Whisper přepis |
| `src/VoiceAssistant.Shared/TextInput/DotoolTextTyper.cs` | Text input (dotool) |
| `src/PushToTalkDictation/EvdevKeyboardMonitor.cs` | Čtení klávesnice |

---

## 📦 GitHub

**Repozitář:** https://github.com/Olbrasoft/VoiceAssistant

**Větve:**
- `main` - produkční větev (vše je zde)

---

## 🎤 Voice Assistant skripty

TTS skripty v `~/voice-assistant/voice-output/`:
- `tts-api.sh` - HTTP API wrapper pro EdgeTTS WebSocket Server
- `tts-simple.sh` - Přímý edge-tts bash skript (fallback)

---

*Tento soubor je určen pro AI agenty pracující na projektu. Obsahuje vše potřebné pro pokračování v práci bez nutnosti procházet kód.*
