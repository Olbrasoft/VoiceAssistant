# Voice Assistant - Quick Reference

## 🎤 Jak používat Speech-to-Text diktování

### Jednoduchý postup:
1. **Řekněte wake word**: "Hey Jarvis" nebo "Alexa"
2. **Počkejte na potvrzení**: Uslyšíte "Ano" nebo "Yes"
3. **Mluvte**: Řekněte svůj text (česky)
4. **Počkejte 3 sekundy ticha**: Nahrávání se automaticky zastaví
5. **Text se napíše**: Do aktuálně aktivního okna

### Příklad:
```
Vy: "Hey Jarvis"
Systém: "Ano"
Vy: "Dobrý den, jak se máte?"
[3 sekundy ticha]
→ Text "Dobrý den, jak se máte?" se napíše do aktivního okna
```

## 🔧 Služby

### Zobrazit stav:
```bash
systemctl --user status wakeword-listener orchestration
```

### Zobrazit logy:
```bash
# Orchestration logy:
journalctl --user -u orchestration.service -f

# WakeWord logy:
journalctl --user -u wakeword-listener.service -f

# Oboje najednou:
journalctl --user -u orchestration.service -u wakeword-listener.service -f
```

### Restart:
```bash
# Restart orchestrace (speech-to-text):
systemctl --user restart orchestration.service

# Restart wake word detekce:
systemctl --user restart wakeword-listener.service

# Restart obou:
systemctl --user restart wakeword-listener.service orchestration.service
```

### Zapnout/Vypnout:
```bash
# Vypnout:
systemctl --user stop orchestration.service

# Zapnout:
systemctl --user start orchestration.service

# Vypnout autostart:
systemctl --user disable orchestration.service

# Zapnout autostart:
systemctl --user enable orchestration.service
```

## 📊 Diagnostika

### Test přepisu řeči:
```bash
# 1. Nahrajte testovací soubor (3 sekundy):
arecord -d 3 -f S16_LE -r 16000 -c 1 /tmp/test.wav

# 2. Přepište ho:
python3 ~/Olbrasoft/VoiceAssistant/scripts/transcribe-audio.py /tmp/test.wav

# Očekávaný výstup:
# {"text": "váš řečený text", "language": "cs", "duration": 3.0}
```

### Test xdotool:
```bash
# Otevřete textový editor a spusťte:
xdotool type "Test text"

# Text by se měl objevit v editoru
```

### Kontrola GPU:
```bash
nvidia-smi

# Mělo by zobrazit použití GPU a VRAM
```

## 🐛 Řešení problémů

### Wake word neslyší:
- Zkontrolujte mikrofon: `pactl list sources short`
- Zkontrolujte hlasitost: `pavucontrol`

### Přepis vrací prázdný text:
- Mluvte jasněji a hlasitěji
- Počkejte 3 sekundy ticha po dokončení řeči
- Zkontrolujte logy: `journalctl --user -u orchestration.service -n 50`

### Text se nepíše:
- Ověřte že máte aktivní textové pole (klikněte do editoru)
- Zkontrolujte že běží X11 (ne Wayland): `echo $XDG_SESSION_TYPE`

### Služba spadla:
```bash
# Zjistit proč:
journalctl --user -u orchestration.service -n 50

# Restart:
systemctl --user restart orchestration.service
```

## 📁 Důležité soubory

- **Orchestration binárky**: `~/voice-assistant/orchestration/`
- **WakeWord binárky**: `~/voice-assistant/wakeword-listener/`
- **Služby**: `~/.config/systemd/user/orchestration.service`
- **Zdrojový kód**: `~/Olbrasoft/VoiceAssistant/`
- **Přepisovací skript**: `~/Olbrasoft/VoiceAssistant/scripts/transcribe-audio.py`
- **Testing guide**: `~/Olbrasoft/VoiceAssistant/TESTING.md`

## 🎯 Tipy

1. **Mluvte přirozeně** - systém zvládá českou diakritiku
2. **Počkejte na "Ano"** - než začnete mluvit
3. **3 sekundy ticha** - je optimální doba pro zastavení nahrávání
4. **Použijte v editoru** - funguje v jakémkoli textovém poli (gedit, VS Code, LibreOffice, ...)
5. **Sledujte logy** - pokud něco nefunguje, logy řeknou proč

## 🚀 Výkon

- **Wake word detekce**: ~50ms latence
- **Nahrávání**: Automatické (3s ticho)
- **Přepis**: 2-5 sekund (závisí na GPU)
- **Psaní textu**: ~100ms

Celková latence: **~3-8 sekund** od dokončení řeči
