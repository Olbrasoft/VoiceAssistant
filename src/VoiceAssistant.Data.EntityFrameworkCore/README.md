# VoiceAssistant.Data.EntityFrameworkCore

Entity Framework Core implementace datové vrstvy pro VoiceAssistant.

## Popis

Tento projekt obsahuje:
- **DbContext** pro přístup k SQLite databázi
- **Entity konfigurace** pomocí Fluent API
- **Migrace** pro verzování schématu
- **CQRS handlery** pro příkazy a dotazy

## Entity

| Entita | Popis |
|--------|-------|
| `TranscriptionLog` | Logy transkripce hlasu |
| `TranscriptionSourceEntity` | Lookup tabulka zdrojů transkripce |
| `Setting` | Nastavení aplikace (klíč-hodnota) |
| `VoiceProfile` | Hlasové profily pro TTS |
| `SpeechLockEntity` | Zámky pro zamezení TTS během nahrávání |
| `SpeechLockSourceEntity` | Lookup tabulka zdrojů zámků |
| `AssistantSpeechState` | Stav TTS přehrávání (singleton) |
| `GroqRouterLog` | Logy rozhodnutí Groq routeru |

## Struktura projektu

```
VoiceAssistant.Data.EntityFrameworkCore/
├── VoiceAssistantDbContext.cs       # Hlavní DbContext
├── DesignTimeDbContextFactory.cs    # Factory pro EF migrations
├── ServiceCollectionExtensions.cs   # DI registrace
├── CommandHandlers/                 # CQRS command handlery
│   ├── SpeechLockCommandHandlers/
│   │   ├── SpeechLockCreateCommandHandler.cs
│   │   └── SpeechLockDeleteCommandHandler.cs
│   ├── TranscriptionLogSaveCommandHandler.cs
│   └── VoiceAssistantDbCommandHandler.cs
├── QueryHandlers/                   # CQRS query handlery
│   ├── SpeechLockQueryHandlers/
│   │   └── SpeechLockExistsQueryHandler.cs
│   └── VoiceAssistantDbQueryHandler.cs
└── Migrations/                      # EF Core migrace
    ├── 20251128181923_AddTranscriptionLog.cs
    ├── 20251128213109_AddSpeechLockSource.cs
    ├── 20251129102006_AddAssistantSpeechState.cs
    └── 20251129125716_AddGroqRouterLogs.cs
```

## Použití

### Registrace v DI

```csharp
builder.Services.AddDbContext<VoiceAssistantDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));
```

### Příklad použití DbContextu

```csharp
public class MyService
{
    private readonly VoiceAssistantDbContext _db;

    public MyService(VoiceAssistantDbContext db)
    {
        _db = db;
    }

    public async Task LogTranscription(string text, TranscriptionSource source)
    {
        _db.TranscriptionLogs.Add(new TranscriptionLog
        {
            Text = text,
            SourceId = (int)source,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
```

## Migrace

### Vytvoření nové migrace

```bash
cd /home/jirka/Olbrasoft/VoiceAssistant/src/VoiceAssistant.Data.EntityFrameworkCore
~/.dotnet/dotnet ef migrations add NazevMigrace
```

### Aplikace migrací

```bash
~/.dotnet/dotnet ef database update
```

### Výpis SQL pro migraci

```bash
~/.dotnet/dotnet ef migrations script
```

## Databáze

- **Typ**: SQLite
- **Umístění**: `/home/jirka/voice-assistant/voice-assistant.db`

## Závislosti

- `Microsoft.EntityFrameworkCore.Sqlite`
- `VoiceAssistant.Shared` (entity a enumy)
- `Olbrasoft.Data.Cqrs.EntityFrameworkCore` (CQRS pattern)

## Testy

Unit testy jsou v projektu `VoiceAssistant.Data.EntityFrameworkCore.Tests` a používají in-memory SQLite databázi.

```bash
cd /home/jirka/Olbrasoft/VoiceAssistant
~/.dotnet/dotnet test --filter "FullyQualifiedName~EntityFrameworkCore"
```
