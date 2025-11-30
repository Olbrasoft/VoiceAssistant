# VoiceAssistant - Agent Instructions

**Poslední aktualizace:** 2025-11-29  
**Stav:** ✅ Plně funkční

Tento soubor obsahuje všechny informace potřebné pro práci na projektu bez nutnosti procházet kód.

---

## 🎯 O projektu

VoiceAssistant je platforma pro hlasové ovládání na Linuxu s těmito komponentami:

| Komponenta | Popis | Port |
|------------|-------|------|
| **ContinuousListener** | Neustálé poslouchání s VAD + Whisper + Groq router | 5051 |
| **Push-to-Talk Dictation** | Drž CapsLock → nahrávej → přepis → vlož text | 5050 |
| **Edge TTS Server** | Microsoft Edge Text-to-Speech přes WebSocket | 5555 |

---

## 📁 Struktura projektu

```
~/Olbrasoft/VoiceAssistant/           # Git repozitář (zdrojový kód)
├── src/
│   ├── VoiceAssistant.Shared/        # Sdílená knihovna
│   │   ├── Speech/                   # OnnxWhisperTranscriber, AudioPreprocessor, TokenDecoder
│   │   ├── TextInput/                # DotoolTextTyper (Wayland text input)
│   │   ├── Data/                     # Entity, Commands, Queries, Enums
│   │   └── Input/                    # CapsLockStateDetector
│   │
│   ├── VoiceAssistant.Data.EntityFrameworkCore/  # EF Core + SQLite
│   │   ├── VoiceAssistantDbContext.cs
│   │   ├── CommandHandlers/          # CQRS command handlery
│   │   ├── QueryHandlers/            # CQRS query handlery
│   │   └── Migrations/               # EF migrace
│   │
│   ├── ContinuousListener/           # Neustálé poslouchání + Groq router
│   │   ├── ContinuousListenerWorker.cs
│   │   ├── Services/
│   │   │   ├── AudioCaptureService.cs      # pw-record audio capture
│   │   │   ├── VadService.cs               # Silero VAD (ONNX)
│   │   │   ├── TranscriptionService.cs     # Whisper přepis
│   │   │   ├── GroqRouterService.cs        # LLM router (OpenCode/Respond/Bash/Ignore)
│   │   │   ├── CommandDispatcher.cs        # Dispatch do OpenCode
│   │   │   ├── TtsPlaybackService.cs       # Přehrávání TTS
│   │   │   ├── BashExecutionService.cs     # Spouštění bash příkazů
│   │   │   └── SpeechLockService.cs        # Zamykání TTS
│   │   └── appsettings.json
│   │
│   ├── PushToTalkDictation/          # Core knihovna
│   │   ├── EvdevKeyboardMonitor.cs   # Čtení klávesnice (evdev)
│   │   ├── AlsaAudioRecorder.cs      # ALSA nahrávání
│   │   └── PwRecordAudioCapture.cs   # PipeWire nahrávání
│   │
│   ├── PushToTalkDictation.Service/  # Worker Service + SignalR hub
│   │   ├── DictationWorker.cs        # Hlavní worker
│   │   ├── Hubs/PttHub.cs            # SignalR hub na :5050/hubs/ptt
│   │   ├── transcription-indicator.py # Python systray indikátor
│   │   └── assets/                   # SVG ikony pro animaci
│   │
│   └── EdgeTtsWebSocketServer/       # TTS server
│       ├── Controllers/SpeechController.cs
│       └── Services/EdgeTtsService.cs
│
├── tests/                            # Unit testy
│   ├── VoiceAssistant.Shared.Tests/
│   ├── VoiceAssistant.Data.EntityFrameworkCore.Tests/
│   ├── PushToTalkDictation.Tests/
│   ├── PushToTalkDictation.Service.Tests/
│   └── EdgeTtsWebSocketServer.Tests/
│
└── VoiceAssistant.sln
```

**Nasazená verze:** `~/voice-assistant/` (viz `~/voice-assistant/AGENTS.md`)

---

## 🎤 ContinuousListener - Hlavní komponenta

### Workflow

```
┌─────────────────┐     ┌─────────────┐     ┌──────────────────┐
│ AudioCapture    │────▶│ VAD (Silero)│────▶│ Whisper          │
│ (pw-record)     │     │ ONNX Model  │     │ Transkripce      │
└─────────────────┘     └─────────────┘     └────────┬─────────┘
                                                      │
                                                      ▼
┌─────────────────┐     ┌─────────────┐     ┌──────────────────┐
│ TTS / Bash      │◀────│ Groq Router │◀────│ Text             │
│ / OpenCode      │     │ (LLM)       │     │                  │
└─────────────────┘     └─────────────┘     └──────────────────┘
```

### Groq Router Actions

| Action | Kdy se použije | Příklad |
|--------|----------------|---------|
| `OPENCODE` | Programovací příkazy, práce s kódem | "Počítači, oprav chybu v testech" |
| `RESPOND` | Jednoduché dotazy (čas, datum, výpočty) | "Kolik je hodin?" |
| `BASH` | Systémové příkazy, otevírání aplikací | "Otevři VS Code" |
| `IGNORE` | Irelevantní řeč, šum | "...tak jo, uvidíme..." |

### Konfigurace (appsettings.json)

```json
{
  "ContinuousListener": {
    "SampleRate": 16000,
    "VadChunkMs": 32,
    "PostSilenceMs": 1500,
    "MinRecordingMs": 800
  },
  "GroqRouter": {
    "ApiKey": "gsk_...",
    "Model": "llama-3.3-70b-versatile"
  },
  "TtsApiUrl": "http://localhost:5555",
  "OpenCodeUrl": "http://localhost:4096"
}
```

---

## 🔌 Služby a porty

| Služba | Port | Endpoint | Systemd unit |
|--------|------|----------|--------------|
| ContinuousListener | 5051 | `http://localhost:5051/health` | `continuous-listener.service` |
| Push-to-Talk Dictation | 5050 | `ws://localhost:5050/hubs/ptt` | `push-to-talk-dictation.service` |
| Edge TTS Server | 5555 | `http://localhost:5555/api/speech/speak` | `edge-tts-server.service` |

**Kontrola služeb:**
```bash
systemctl --user status continuous-listener
systemctl --user status edge-tts-server
journalctl --user -u continuous-listener -f
```

---

## 🛠️ Vývoj a deployment

### Build & Test

```bash
cd ~/Olbrasoft/VoiceAssistant
~/.dotnet/dotnet build
~/.dotnet/dotnet test
```

### Deploy ContinuousListener

```bash
cd ~/Olbrasoft/VoiceAssistant
~/.dotnet/dotnet publish src/ContinuousListener -c Release \
  -o ~/voice-assistant/continuous-listener
systemctl --user restart continuous-listener
```

### Deploy Edge TTS Server

```bash
~/.dotnet/dotnet publish src/EdgeTtsWebSocketServer -c Release \
  -o ~/voice-assistant/edge-tts-websocket-server
systemctl --user restart edge-tts-server
```

### Deploy Push-to-Talk Dictation

```bash
./deploy-push-to-talk-dictation.sh
# nebo ručně:
~/.dotnet/dotnet publish src/PushToTalkDictation.Service -c Release \
  -o ~/voice-assistant/push-to-talk-dictation-service
systemctl --user restart push-to-talk-dictation
```

---

## 🗄️ Databáze (SQLite + EF Core)

**Umístění:** `~/voice-assistant/voice-assistant.db`

### Entity

| Entity | Tabulka | Popis |
|--------|---------|-------|
| `TranscriptionLog` | TranscriptionLogs | Historie přepisů řeči |
| `GroqRouterLog` | GroqRouterLogs | Rozhodnutí Groq routeru |
| `SpeechLockEntity` | SpeechLocks | Zámky TTS během nahrávání |
| `AssistantSpeechState` | AssistantSpeechStates | Stav TTS přehrávání |
| `Setting` | Settings | Konfigurace (klíč-hodnota) |
| `VoiceProfile` | VoiceProfiles | Hlasové profily |

### EF Core migrace

```bash
cd ~/Olbrasoft/VoiceAssistant/src/VoiceAssistant.Data.EntityFrameworkCore
~/.dotnet/dotnet ef migrations add NazevMigrace
~/.dotnet/dotnet ef database update
```

---

## ⚙️ Technologie

- **.NET 10** (Preview) - SDK a runtime
- **ASP.NET Core** - Web API, SignalR
- **Entity Framework Core** - SQLite ORM
- **ONNX Runtime CUDA** - GPU-akcelerovaný Whisper přepis
- **Silero VAD** - Voice Activity Detection (ONNX)
- **Groq API** - LLM router (llama-3.3-70b)
- **pw-record** - PipeWire audio capture
- **dotool** - Wayland text input
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

## 🔗 Klíčové soubory

| Soubor | Účel |
|--------|------|
| `src/ContinuousListener/ContinuousListenerWorker.cs` | Hlavní smyčka - VAD → Whisper → Router |
| `src/ContinuousListener/Services/GroqRouterService.cs` | Groq LLM router |
| `src/ContinuousListener/Services/CommandDispatcher.cs` | Dispatch příkazů do OpenCode |
| `src/VoiceAssistant.Shared/Speech/OnnxWhisperTranscriber.cs` | Whisper přepis (ONNX) |
| `src/VoiceAssistant.Shared/TextInput/DotoolTextTyper.cs` | Text input (dotool) |
| `src/EdgeTtsWebSocketServer/Services/EdgeTtsService.cs` | TTS přes Microsoft Edge |
| `src/PushToTalkDictation.Service/DictationWorker.cs` | PTT worker |

---

## 📦 GitHub

**Repozitář:** https://github.com/Olbrasoft/VoiceAssistant

**Větve:**
- `main` - produkční větev

---

## 📋 Možná budoucí vylepšení

- [ ] Podpora více jazyků (ne jen čeština)
- [ ] Konfigurovatelná klávesa pro PTT (ne jen CapsLock)
- [ ] GUI pro nastavení
- [ ] Wake word detekce offline (místo Groq routeru)
- [ ] Konverzační paměť (multi-turn)

---

*Tento soubor je určen pro AI agenty pracující na projektu. Pro nasazenou verzi viz `~/voice-assistant/AGENTS.md`.*
