# WakeWordDetection.Service

Web service that exposes wake word detection via HTTP/SignalR.

## Features

- ASP.NET Core Web API
- Real-time wake word detection
- SignalR hub for notifications
- Swagger documentation

## Endpoints

- `GET /health` - Health check
- `POST /start` - Start detection
- `POST /stop` - Stop detection
- SignalR Hub: `/wakeword` - Real-time notifications

## Running

### Development
```bash
cd src/WakeWordDetection.Service
dotnet run
```

### Production
```bash
dotnet publish -c Release
./WakeWordDetection.Service
```

## Configuration

Configure in `appsettings.json`:
- Model path
- Audio device
- Detection sensitivity

## Dependencies

- **WakeWordDetection** - Core library
- **Swashbuckle.AspNetCore** - Swagger/OpenAPI

## Integration

The Orchestration service connects to this service to receive wake word notifications and trigger voice command processing.
