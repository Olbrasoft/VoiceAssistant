# 📊 Push-to-Talk Dictation - Stav projektu

**Poslední aktualizace:** 2025-11-28  
**Status:** ✅ DOKONČENO

---

## ✅ CO JE HOTOVO

### 1. Migrace z Whisper.net na vlastní ONNX implementaci
- ✅ Whisper.net měl problémy s CUDA (GGML assertion failure)
- ✅ sherpa-onnx NuGet balíček nefungoval správně
- ✅ Vytvořena vlastní implementace s ONNX Runtime + CUDA

### 2. Vlastní ONNX Whisper implementace
- ✅ `OnnxWhisperTranscriber.cs` - hlavní transcriber
- ✅ `AudioPreprocessor.cs` - mel spectrogram s oficiálními filtry
- ✅ `TokenDecoder.cs` - BPE dekodér s podporou češtiny
- ✅ CUDA GPU akcelerace funguje
- ✅ Podpora více modelů (tiny/base/small/medium/large)

### 3. Chunking pro delší nahrávky
- ✅ Audio > 30s se rozdělí na chunky
- ✅ 1s overlap mezi chunky
- ✅ Max 10 chunků (5 minut)
- ✅ Výsledky se spojují

### 4. Token suppression pro lepší kvalitu
- ✅ Potlačení timestamp tokenů (50364-50864)
- ✅ Potlačení language tokenů (50259-50357)
- ✅ Potlačení speciálních tokenů (translate, transcribe, nospeech)

### 5. Deployment
- ✅ Systemd user service funguje
- ✅ Automatický start při přihlášení
- ✅ Konfigurace v appsettings.json

---

## 📦 Aktuální konfigurace

**Model:** Whisper Small  
**Rychlost:** ~1s na 30s chunk  
**Jazyk:** Čeština (cs)

**Cesta k modelu:**
```
/home/jirka/voice-assistant/push-to-talk-dictation/models/sherpa-onnx-whisper-small/
```

---

## 🔧 Klíčové soubory

### Zdrojový kód
```
~/Olbrasoft/VoiceAssistant/src/
├── VoiceAssistant.Shared/Speech/
│   ├── OnnxWhisperTranscriber.cs  # ONNX Whisper s GPU
│   ├── AudioPreprocessor.cs        # Mel spectrogram
│   └── TokenDecoder.cs             # BPE dekodér
├── PushToTalkDictation/            # Core library
└── PushToTalkDictation.Service/    # Systemd service
```

### Deployment
```
~/voice-assistant/push-to-talk-dictation/
├── PushToTalkDictation.Service.dll
├── VoiceAssistant.Shared.dll
├── appsettings.json
└── models/
    ├── sherpa-onnx-whisper-small/
    └── sherpa-onnx-whisper-medium/
```

---

## 🚀 Příkazy

### Build & Deploy
```bash
cd ~/Olbrasoft/VoiceAssistant
dotnet publish src/PushToTalkDictation.Service/PushToTalkDictation.Service.csproj \
  -c Release -o ~/voice-assistant/push-to-talk-dictation/
systemctl --user restart push-to-talk-dictation
```

### Změna modelu
Edituj `appsettings.json`:
- Small: `.../models/sherpa-onnx-whisper-small`
- Medium: `.../models/sherpa-onnx-whisper-medium`

---

## 📝 Historie

| Datum | Změna |
|-------|-------|
| 2025-11-27 | Migrace z Python na C# .NET |
| 2025-11-27 | Vlastní ONNX Whisper implementace |
| 2025-11-28 | Oprava mel spectrogram (oficiální filtry) |
| 2025-11-28 | Oprava zacyklení (token suppression) |
| 2025-11-28 | Chunking pro delší nahrávky |
| 2025-11-28 | Token suppression pro lepší kvalitu |

---

**Status:** ✅ Projekt dokončen a funkční
