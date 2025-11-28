# Voice Assistant → OpenCode - Quick Start

## ✅ Co je hotovo

Voice Assistant nyní **plně integrován s OpenCode**:
- 📡 HTTP API komunikace (žádné xdotool!)
- ⏎ Automatické odeslání promptu (Enter)
- 🔊 Wake word detection funguje
- 🎤 Hlasové diktování → přímý přenos do OpenCode

## 🚀 Jak to použít

### 1. Standardní workflow (Wake Word)

**Řekni:** "Jarvis" nebo "Alexa"
- Uslyšíš "yes.mp3" potvrzení
- Řekni svůj příkaz (5 sekund nahrávání)
- Text se automaticky přepíše a odešle do OpenCode
- OpenCode rovnou vykoná příkaz

**Příklad:**
```
Uživatel: "Jarvis"
Systém: *pííp* (yes.mp3)
Uživatel: "Vytvoř funkci getUserData"
→ Text se objeví v OpenCode a rovnou se odešle
→ OpenCode začne vytvářet funkci
```

### 2. Test přes API

```bash
# Test s odesláním
~/test-voice-opencode.sh "Test zpráva" true

# Test bez odeslání (jen přidá do promptu)
~/test-voice-opencode.sh "Test zpráva" false
```

### 3. Manuální trigger

```bash
# Spustí celý workflow (nahrávání + přepis + odeslání)
curl -X POST http://localhost:5200/api/voice/dictate
```

## ⚙️ Konfigurace

**Soubor:** `~/voice-assistant/orchestration/appsettings.json`

```json
{
  "OpenCodeUrl": "http://localhost:36277",
  "OpenCodeAutoSubmit": true
}
```

**Nastavení:**
- `OpenCodeAutoSubmit: true` - Automaticky odešle (stiskne Enter)
- `OpenCodeAutoSubmit: false` - Pouze přidá text, neodešle

## 🔧 Služby

### Kontrola stavu
```bash
systemctl status orchestration.service
systemctl status wakeword-listener.service
```

### Restart
```bash
sudo systemctl restart orchestration.service
```

### Logy
```bash
journalctl -u orchestration.service -f
```

## 📊 Porty

- **5000** - WakeWord Detection (SignalR)
- **5200** - Orchestration API (REST)
- **36277** - OpenCode HTTP API

## 🎯 Use Cases

### Příkazy (Auto-Submit ON)
Pro okamžité vykonání příkazů:
- "Vytvoř funkci..."
- "Oprav chybu v..."
- "Přidej test pro..."

### Diktování (Auto-Submit OFF)
Pro psaní textu, který chceš editovat:
- Komentáře
- Dokumentace
- Dlouhé texty

**Změna:** Edituj `OpenCodeAutoSubmit` v `appsettings.json` a restartuj službu.

## 🐛 Troubleshooting

### OpenCode nepřijímá text
```bash
# Zkontroluj, jestli OpenCode běží
ps aux | grep opencode

# Zjisti port
ss -tlnp | grep opencode

# Aktualizuj port v appsettings.json
```

### Wake word nefunguje
```bash
# Zkontroluj službu
systemctl status wakeword-listener.service

# Zkontroluj logy
journalctl -u wakeword-listener.service -n 50
```

### Audio nefunguje
```bash
# Test nahrávání
arecord -d 3 test.wav

# Zkontroluj audio zařízení
arecord -l
```

## 📝 Next Steps

Po základním nastavení můžeš:
1. Upravit wake word (v konfiguraci WakeWordDetection)
2. Změnit dobu nahrávání (v SpeechRecognitionService)
3. Přidat vlastní audio potvrzení (nahraď yes.mp3)
4. Integrovat s dalšími aplikacemi přes API

## 📚 Dokumentace

Detailní informace:
- `OPENCODE_INTEGRATION.md` - Plná dokumentace integrace
- `~/test-voice-opencode.sh` - Test script s příklady
