# Voice Assistant → OpenCode Integration

## Overview

Voice Assistant Orchestration service integrates directly with OpenCode via HTTP API, eliminating the need for xdotool and window focus management. **Now supports automatic prompt submission (Enter key)!**

## Architecture

```
Voice Input → Orchestration Service → OpenCode HTTP API
                    ↓
            Port 5200 (REST API)
                    ↓
            Port 36277 (OpenCode)
                    ↓
            1. /tui/append-prompt (send text)
            2. /tui/submit-prompt (press Enter)
```

## Components

### 1. Orchestration Service
- **Location**: `~/voice-assistant/orchestration/`
- **Service**: `orchestration.service`
- **Port**: 5200
- **Status**: `systemctl status orchestration.service`

### 2. OpenCode Integration
- **Method**: HTTP POST to `/tui/append-prompt` + `/tui/submit-prompt`
- **Port**: 36277 (dynamically assigned by OpenCode)
- **Fallback**: xdotool typing + Enter if OpenCode unavailable
- **Auto-submit**: Configurable via `OpenCodeAutoSubmit` setting

### 3. Configuration
File: `~/voice-assistant/orchestration/appsettings.json`

```json
{
  "OpenCodeUrl": "http://localhost:36277",
  "OpenCodeAutoSubmit": true,
  "WakeWordServiceUrl": "http://localhost:5000"
}
```

**Settings:**
- `OpenCodeUrl` - OpenCode HTTP API endpoint
- `OpenCodeAutoSubmit` - If true, automatically submits prompt after sending text (default: true)

## API Endpoints

### Orchestration API (Port 5200)

#### GET /api/voice/status
Health check endpoint
```bash
curl http://localhost:5200/api/voice/status
```

Response:
```json
{
  "success": true,
  "service": "Orchestration Voice Assistant",
  "status": "running",
  "timestamp": "2025-11-26T20:23:47Z"
}
```

#### POST /api/voice/dictate?submit={true|false}
Manually trigger voice dictation workflow
```bash
# With auto-submit (default)
curl -X POST http://localhost:5200/api/voice/dictate

# Without submit (just append text)
curl -X POST "http://localhost:5200/api/voice/dictate?submit=false"
```

## Workflow

### Normal Operation (Wake Word)
1. User says wake word ("Jarvis" or "Alexa")
2. Orchestration plays "yes.mp3" confirmation
3. Records audio for 5 seconds
4. Transcribes speech with Whisper
5. **Sends text to OpenCode via HTTP** (no xdotool!)
6. **Automatically submits prompt** (presses Enter)
7. OpenCode executes the prompt

### Manual Trigger (API)
1. External app calls `/api/voice/dictate`
2. Same workflow as above

### Fallback Mode
If OpenCode HTTP API is unavailable:
- Automatically falls back to xdotool typing
- Uses `xdotool type` for text
- Uses `xdotool key Return` for Enter
- Logs warning and continues

## Testing

### Test Script
```bash
# Send and submit (default)
~/test-voice-opencode.sh "Your test message"

# Send without submit
~/test-voice-opencode.sh "Your test message" false

# Send and submit explicitly
~/test-voice-opencode.sh "Your test message" true
```

### Manual Test - Append Only
```bash
# Send text without submitting
curl -X POST http://localhost:36277/tui/append-prompt \
  -H "Content-Type: application/json" \
  -d '{"text": "Test message"}'
```

### Manual Test - Append and Submit
```bash
# Send text
curl -X POST http://localhost:36277/tui/append-prompt \
  -H "Content-Type: application/json" \
  -d '{"text": "Test message"}'

# Submit prompt (press Enter)
curl -X POST http://localhost:36277/tui/submit-prompt
```

## Service Management

```bash
# Restart orchestration
sudo systemctl restart orchestration.service

# View logs
journalctl -u orchestration.service -f

# Check status
systemctl status orchestration.service
```

## Integration Benefits

✅ **No window focus needed** - Direct API communication  
✅ **Automatic submission** - Text is sent and executed immediately  
✅ **Reliable delivery** - HTTP protocol with error handling  
✅ **Fallback support** - xdotool if OpenCode unavailable  
✅ **Clean architecture** - RESTful API design  
✅ **Browser-safe** - Works even when browser is focused  
✅ **Configurable** - Enable/disable auto-submit per use case  

## Use Cases

### 1. Voice Commands (Auto-Submit ON)
**Configuration:** `OpenCodeAutoSubmit: true`

User says: "Jarvis, create a new function called getUserData"
→ Text appears in OpenCode and executes immediately

### 2. Voice Dictation (Auto-Submit OFF)
**Configuration:** `OpenCodeAutoSubmit: false`

User says: "Jarvis, this is a comment I want to add"
→ Text appears in OpenCode but doesn't execute
→ User can review and edit before pressing Enter manually

## Future Enhancements

- [ ] Auto-discover OpenCode port (currently hardcoded)
- [ ] Support multiple OpenCode instances
- [ ] WebSocket support for bidirectional communication
- [ ] Custom OpenCode commands beyond append-prompt
- [ ] Voice feedback after command execution
- [ ] Command history and undo functionality

## Files Modified

- `src/Orchestration/Services/TextInputService.cs` - Added OpenCode HTTP integration with submit support
- `src/Orchestration/Controllers/VoiceController.cs` - REST API endpoints with submit parameter
- `src/Orchestration/Program.cs` - ASP.NET Core Web API setup
- `src/Orchestration/IOrchestrator.cs` - TriggerDictationAsync method
- `src/Orchestration/Orchestrator.cs` - Manual dictation workflow with configurable submit
- `src/Orchestration/appsettings.json` - OpenCode URL and auto-submit configuration

## Deployment

```bash
# Build and publish
cd ~/Olbrasoft/VoiceAssistant/src/Orchestration
dotnet build -c Release
dotnet publish -c Release -o ~/voice-assistant/orchestration

# Restart service
sudo systemctl restart orchestration.service
```

## OpenCode Documentation

- API: https://opencode.ai/docs/server
- SDK: https://opencode.ai/docs/sdk
- Custom Tools: https://opencode.ai/docs/custom-tools
- TUI Control: https://opencode.ai/docs/server#tui

## Technical Details

### Submit Workflow
1. **Append Text**: `POST /tui/append-prompt` with `{"text": "..."}`
2. **Small Delay**: 100ms to ensure text is appended
3. **Submit Prompt**: `POST /tui/submit-prompt` (no body required)

### Error Handling
- HTTP timeouts: 5 seconds
- Automatic fallback to xdotool on connection failure
- Detailed logging for debugging
- Graceful degradation

### Logging
All operations are logged with emoji indicators:
- 📡 Sending to OpenCode
- ✅ Success
- ❌ Error
- ⌨️ Fallback to xdotool
