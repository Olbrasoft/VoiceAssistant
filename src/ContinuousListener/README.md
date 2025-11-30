# ContinuousListener

Služba pro kontinuální naslouchání hlasovým příkazům s inteligentním routováním přes Groq LLM.

## Popis

ContinuousListener je .NET Worker Service, která:

1. **Zachytává audio** z mikrofonu v reálném čase
2. **Detekuje řeč** pomocí Silero VAD (Voice Activity Detection) neuronové sítě
3. **Transkribuje** mluvenou řeč pomocí Whisper modelu
4. **Routuje** příkazy přes Groq LLM, který rozhodne o akci:
   - `OPENCODE` - příkaz pro programování → posílá do OpenCode
   - `RESPOND` - jednoduchý dotaz → odpovídá přes TTS
   - `IGNORE` - irelevantní řeč → ignoruje

## Architektura

```
┌─────────────────┐     ┌─────────────┐     ┌──────────────────┐
│ AudioCapture    │────▶│ VAD (Silero)│────▶│ Transcription    │
│ (ALSA/PulseAudio)│     │ ONNX Model  │     │ (Whisper)        │
└─────────────────┘     └─────────────┘     └────────┬─────────┘
                                                      │
                                                      ▼
┌─────────────────┐     ┌─────────────┐     ┌──────────────────┐
│ TTS Playback    │◀────│ Groq Router │◀────│ Text             │
│ (EdgeTTS)       │     │ (LLM)       │     │                  │
└─────────────────┘     └──────┬──────┘     └──────────────────┘
                               │
                               ▼
                        ┌─────────────┐
                        │ OpenCode    │
                        │ Dispatcher  │
                        └─────────────┘
```

## Služby

| Služba | Popis |
|--------|-------|
| `AudioCaptureService` | Zachytávání audio z mikrofonu (16kHz, mono, 16-bit) |
| `VadService` | Voice Activity Detection pomocí Silero ONNX modelu |
| `TranscriptionService` | Převod řeči na text pomocí Whisper |
| `GroqRouterService` | Inteligentní routování přes Groq LLM API |
| `TtsPlaybackService` | Přehrávání TTS odpovědí přes EdgeTTS server |
| `TtsControlService` | Ovládání TTS serveru (stop) |
| `CommandDispatcher` | Odesílání příkazů do OpenCode |
| `SpeechLockService` | Zamykání TTS během nahrávání |
| `AssistantSpeechStateService` | Sledování stavu TTS přehrávání |

## Konfigurace

```json
{
  "ContinuousListener": {
    "SampleRate": 16000,
    "VadChunkMs": 32,
    "PreBufferMs": 1000,
    "PostSilenceMs": 600,
    "MinRecordingMs": 500
  },
  "OpenCodeUrl": "http://localhost:4096",
  "TtsApiUrl": "http://localhost:5555",
  "GroqRouter": {
    "ApiKey": "gsk_...",
    "Model": "llama-3.3-70b-versatile"
  }
}
```

## Spuštění

```bash
cd /home/jirka/voice-assistant/continuous-listener
~/.dotnet/dotnet ContinuousListener.dll
```

## Závislosti

- **EdgeTTS WebSocket Server** (port 5555) - pro TTS přehrávání
- **OpenCode** (port 4096) - pro příjem programovacích příkazů
- **Groq API** - pro inteligentní routování

## Workflow

1. Audio je kontinuálně zachytáváno a analyzováno VAD
2. Když je detekována řeč, začne se nahrávat
3. Po detekci ticha (600ms) se nahrávka transkribuje
4. Transkript je odeslán do Groq Router
5. Groq rozhodne o akci:
   - **OpenCode**: příkaz je odeslán do OpenCode API
   - **Respond**: odpověď je přehrána přes TTS
   - **Ignore**: nic se neděje
6. Systém se vrací do stavu čekání

## Vývoj

```bash
# Build
cd /home/jirka/Olbrasoft/VoiceAssistant/src/ContinuousListener
~/.dotnet/dotnet build

# Publish
~/.dotnet/dotnet publish -c Release -o /home/jirka/voice-assistant/continuous-listener
```
