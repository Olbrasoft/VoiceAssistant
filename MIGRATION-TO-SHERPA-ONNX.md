# Migrace VoiceAssistant na sherpa-onnx

**Datum vytvoření:** 27. listopadu 2025  
**Stav:** 🚧 PŘIPRAVENO K IMPLEMENTACI

---

## 🎯 Cíl migrace

Nahradit **Whisper.net** (problematický CUDA backend) za **sherpa-onnx** (stabilní, multiplatformní).

### Problém, který řešíme

```
GGML_ASSERT(prev != ggml_uncaught_exception) failed
```

- **Příčina:** GGML threading issue s Whisper.net CUDA runtime
- **Dopad:** Aplikace crashuje při načítání Whisper modelu s GPU
- **Řešení:** Migrace na sherpa-onnx (stabilní ONNX Runtime, žádné GGML závislosti)

---

## 📦 Technické informace

### Hardware
- **GPU:** NVIDIA GeForce RTX 3060 (8GB VRAM, Driver 550.163.01)
- **OS:** Debian GNU/Linux 13 (Trixie)
- **Runtime:** .NET 10.0
- **Audio:** 16kHz, 1 channel, 16-bit PCM
- **Disk:** 739GB volného místa

### Současné balíčky (Whisper.net)
```xml
<PackageReference Include="Whisper.net" Version="1.9.0" />
<PackageReference Include="Whisper.net.Runtime.Cuda" Version="1.9.0" />
```

### Nové balíčky (sherpa-onnx)
```xml
<PackageReference Include="org.k2fsa.sherpa.onnx" Version="1.12.17" />
```

**NuGet Package:** [`org.k2fsa.sherpa.onnx`](https://www.nuget.org/packages/org.k2fsa.sherpa.onnx)  
**Dokumentace:** [https://k2-fsa.github.io/sherpa/onnx/](https://k2-fsa.github.io/sherpa/onnx/)  
**GitHub:** [https://github.com/k2-fsa/sherpa-onnx](https://github.com/k2-fsa/sherpa-onnx)

---

## 🗂️ Struktura projektu

### VoiceAssistant.Shared (Knihovna)
```
/home/jirka/Olbrasoft/VoiceAssistant/src/VoiceAssistant.Shared/
├── VoiceAssistant.Shared.csproj          ← Upravíme PackageReference
├── Speech/
│   ├── ISpeechTranscriber.cs             ← Zachováme (interface)
│   ├── TranscriptionResult.cs            ← Zachováme (model)
│   ├── WhisperTranscriber.cs             ← SMAŽEME
│   └── SherpaTranscriber.cs              ← VYTVOŘÍME (nový)
```

### PushToTalkDictation.Service (Aplikace)
```
/home/jirka/Olbrasoft/VoiceAssistant/src/PushToTalkDictation.Service/
├── PushToTalkDictation.Service.csproj    ← Bez změn
├── Program.cs                             ← Upravíme DI registraci
├── DictationWorker.cs                     ← Upravíme použití API
```

### Deployment
```
/home/jirka/voice-assistant/push-to-talk-dictation/
├── appsettings.json                       ← Upravíme model path
├── models/
│   ├── ggml-medium.bin                    ← Stará (Whisper.net)
│   └── sherpa-onnx-whisper-medium/        ← Nová (sherpa-onnx)
│       ├── medium-encoder.onnx
│       ├── medium-decoder.onnx
│       └── medium-tokens.txt
```

---

## 📝 Plán implementace

### FÁZE 1: Příprava prostředí ⏱️ 15 min

#### 1.1 Stažení sherpa-onnx Whisper medium modelu
```bash
cd /home/jirka/voice-assistant/push-to-talk-dictation/models/
wget https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/sherpa-onnx-whisper-medium.tar.bz2
tar xvf sherpa-onnx-whisper-medium.tar.bz2
rm sherpa-onnx-whisper-medium.tar.bz2
```

**Velikost:** ~1.5 GB (compressed), ~3 GB (extracted)

#### 1.2 Ověření struktury modelu
```bash
ls -lh sherpa-onnx-whisper-medium/
# Očekávané soubory:
# - medium-encoder.onnx
# - medium-decoder.onnx  
# - medium-tokens.txt
# - test_wavs/ (testovací audio)
```

---

### FÁZE 2: Update VoiceAssistant.Shared ⏱️ 30 min

#### 2.1 Aktualizace `.csproj`
```bash
cd /home/jirka/Olbrasoft/VoiceAssistant/src/VoiceAssistant.Shared/
# Editovat VoiceAssistant.Shared.csproj
```

**Změny:**
```xml
<!-- ODEBRAT -->
<PackageReference Include="Whisper.net" Version="1.9.0" />
<PackageReference Include="Whisper.net.Runtime.Cuda" Version="1.9.0" />

<!-- PŘIDAT -->
<PackageReference Include="org.k2fsa.sherpa.onnx" Version="1.12.17" />
```

#### 2.2 Vytvořit `SherpaTranscriber.cs`

**Lokace:** `/home/jirka/Olbrasoft/VoiceAssistant/src/VoiceAssistant.Shared/Speech/SherpaTranscriber.cs`

**Implementace:** Nový transcriber používající sherpa-onnx API

**Klíčové změny API:**

| Aspekt | Whisper.net (staré) | sherpa-onnx (nové) |
|--------|---------------------|---------------------|
| **Input** | `Stream` (WAV) | `float[]` samples |
| **Output** | `IAsyncEnumerable<SegmentData>` | `string` (direct text) |
| **Model** | Single `.bin` (GGML) | 2× `.onnx` + tokens |
| **Language** | `.WithLanguage("cs")` | `Language = "cs"` v config |
| **GPU** | CUDA (nestabilní) | Provider = "cuda" (stabilní) |

**Signatury API:**
```csharp
// STARÁ (Whisper.net)
Task<TranscriptionResult> TranscribeAsync(Stream audioStream, CancellationToken ct);

// NOVÁ (sherpa-onnx)
Task<TranscriptionResult> TranscribeAsync(byte[] pcmData, CancellationToken ct);
Task<TranscriptionResult> TranscribeAsync(float[] audioSamples, CancellationToken ct);
```

#### 2.3 Smazat `WhisperTranscriber.cs`
```bash
rm /home/jirka/Olbrasoft/VoiceAssistant/src/VoiceAssistant.Shared/Speech/WhisperTranscriber.cs
```

#### 2.4 Build a test
```bash
cd /home/jirka/Olbrasoft/VoiceAssistant/src/VoiceAssistant.Shared/
dotnet build
```

---

### FÁZE 3: Update PushToTalkDictation.Service ⏱️ 20 min

#### 3.1 Upravit `Program.cs`

**Lokace:** `/home/jirka/Olbrasoft/VoiceAssistant/src/PushToTalkDictation.Service/Program.cs`

**Změny (řádky 23-27):**
```csharp
// PŘED (Whisper.net)
builder.Services.AddSingleton<ISpeechTranscriber>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<WhisperTranscriber>>();
    return new WhisperTranscriber(logger, whisperModelPath, whisperLanguage);
});

// PO (sherpa-onnx)
builder.Services.AddSingleton<ISpeechTranscriber>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<SherpaTranscriber>>();
    return new SherpaTranscriber(logger, whisperModelPath, whisperLanguage);
});
```

#### 3.2 Upravit `DictationWorker.cs`

**Lokace:** `/home/jirka/Olbrasoft/VoiceAssistant/src/PushToTalkDictation.Service/DictationWorker.cs`

**Změny v metodě `StopRecordingAsync()` (řádky 162-173):**

```csharp
// PŘED (řádky 167-173)
// Convert raw PCM to WAV format (Whisper.net requires WAV with RIFF header)
_logger.LogInformation("Converting PCM to WAV format...");
using var wavStream = ConvertPcmToWav(recordedData, 16000, 1, 16);

// Transcribe audio to text
_logger.LogInformation("Starting transcription...");
var transcription = await _speechTranscriber.TranscribeAsync(wavStream);

// PO (sherpa-onnx)
// Transcribe raw PCM directly (sherpa-onnx accepts byte[] PCM)
_logger.LogInformation("Starting transcription...");
var transcription = await _speechTranscriber.TranscribeAsync(recordedData);
```

**Odstranit metodu `ConvertPcmToWav()` (řádky 219-259):**
- sherpa-onnx zpracovává PCM přímo
- WAV header již není potřeba

#### 3.3 Update `appsettings.json`

**Lokace:** `/home/jirka/voice-assistant/push-to-talk-dictation/appsettings.json`

**Změny:**
```json
{
  "PushToTalkDictation": {
    // PŘED
    "WhisperModelPath": "/home/jirka/voice-assistant/push-to-talk-dictation/models/ggml-medium.bin",
    
    // PO
    "WhisperModelPath": "/home/jirka/voice-assistant/push-to-talk-dictation/models/sherpa-onnx-whisper-medium",
    
    "WhisperLanguage": "cs"
  }
}
```

---

### FÁZE 4: Build & Deploy ⏱️ 15 min

#### 4.1 Build solution
```bash
cd /home/jirka/Olbrasoft/VoiceAssistant/
dotnet build --configuration Release
```

#### 4.2 Publish PushToTalkDictation.Service
```bash
cd /home/jirka/Olbrasoft/VoiceAssistant/src/PushToTalkDictation.Service/
dotnet publish -c Release -o /home/jirka/voice-assistant/push-to-talk-dictation/ \
  --self-contained false --runtime linux-x64
```

#### 4.3 Restart systemd service
```bash
sudo systemctl restart push-to-talk-dictation.service
sudo systemctl status push-to-talk-dictation.service
```

---

### FÁZE 5: Test & Verify ⏱️ 10 min

#### 5.1 Test CapsLock → nahrávání → přepis
```bash
# Monitor logs
journalctl -u push-to-talk-dictation.service -f
```

**Test postup:**
1. Stisknout CapsLock (začne nahrávání)
2. Mluvit česky
3. Pustit CapsLock (stop + přepis)
4. Ověřit text v aktivním okně

#### 5.2 Ověřit GPU utilization
```bash
watch -n 1 nvidia-smi
```

**Očekávaný výstup během přepisu:**
- GPU utilization: 30-70%
- Memory usage: 1-2 GB

#### 5.3 Monitor logs pro chyby
```bash
journalctl -u push-to-talk-dictation.service -n 50 --no-pager
```

**Hledat:**
- ✅ "Whisper model loaded successfully"
- ✅ "Transcription successful"
- ❌ GGML errors (neměly by se objevit)
- ❌ CUDA crashes

---

## 🔑 Klíčové změny API

### Konverze PCM → float[]

**Nová metoda v `SherpaTranscriber.cs`:**
```csharp
private static float[] ConvertPcmToFloat32(byte[] pcmData)
{
    var samples = new float[pcmData.Length / 2];
    for (int i = 0; i < samples.Length; i++)
    {
        short sample = BitConverter.ToInt16(pcmData, i * 2);
        samples[i] = sample / 32768.0f; // Normalize to [-1.0, 1.0]
    }
    return samples;
}
```

### sherpa-onnx OfflineRecognizer Config

```csharp
var config = new OfflineRecognizerConfig
{
    ModelConfig = new OfflineModelConfig
    {
        Whisper = new OfflineWhisperModelConfig
        {
            Encoder = Path.Combine(modelPath, "medium-encoder.onnx"),
            Decoder = Path.Combine(modelPath, "medium-decoder.onnx"),
            Language = "cs",
            TailPaddings = 1000  // Czech speech processing
        },
        Tokens = Path.Combine(modelPath, "medium-tokens.txt"),
        Provider = "cuda",  // GPU acceleration
        NumThreads = 4,
        Debug = false
    }
};

var recognizer = new OfflineRecognizer(config);
```

---

## ⚠️ Potenciální problémy

### 1. NuGet balíček neobsahuje native libraries
**Řešení:** Balíček `org.k2fsa.sherpa.onnx` automaticky stáhne runtime dependencies pro `linux-x64`

### 2. Model není kompatibilní
**Řešení:** Použít oficiální sherpa-onnx exportovaný model z GitHub releases

### 3. CUDA není dostupná
**Řešení:** Fallback na CPU (`Provider = "cpu"`)

### 4. Nižší přesnost oproti Whisper.net
**Řešení:** Obě používají Whisper medium - přesnost by měla být identická

---

## 📊 Srovnání: Whisper.net vs sherpa-onnx

| Kritérium | Whisper.net | sherpa-onnx |
|-----------|-------------|-------------|
| **Stars** | 834 | 9,039 |
| **CUDA stabilita** | ⚠️ Nestabilní (GGML crash) | ✅ Stabilní |
| **Runtime** | whisper.cpp | ONNX Runtime |
| **Threading** | ❌ GGML assert | ✅ Bezproblémové |
| **GPU Support** | CUDA | CUDA/DirectML/Vulkan |
| **Model formát** | `.bin` (GGML) | `.onnx` |
| **Čeština** | ✅ Podporováno | ✅ Podporováno |
| **API složitost** | 🟢 Nízká | 🟢 Nízká |
| **.NET verze** | .NET 6+ | .NET 6+ |
| **Komunita** | Menší | Velmi aktivní |

---

## 🚀 Další kroky po migraci

1. **Optimalizace:** Vyzkoušet int8 kvantizované modely (rychlejší inference)
2. **Monitoring:** Přidat metriky pro transcription latency
3. **Fallback:** Implementovat automatický fallback CPU ↔ GPU
4. **Testing:** Přidat unit testy pro `SherpaTranscriber`
5. **Dokumentace:** Aktualizovat README.md

---

## 📚 Reference

- [sherpa-onnx GitHub](https://github.com/k2-fsa/sherpa-onnx)
- [sherpa-onnx Dokumentace](https://k2-fsa.github.io/sherpa/onnx/)
- [NuGet balíček](https://www.nuget.org/packages/org.k2fsa.sherpa.onnx)
- [Whisper modely](https://github.com/k2-fsa/sherpa-onnx/releases/tag/asr-models)
- [C# příklady](https://github.com/k2-fsa/sherpa-onnx/tree/master/dotnet-examples)

---

**Vytvořeno:** 2025-11-27 21:25 CET  
**Poslední aktualizace:** 2025-11-27 21:25 CET  
**Autor:** OpenCode Agent
