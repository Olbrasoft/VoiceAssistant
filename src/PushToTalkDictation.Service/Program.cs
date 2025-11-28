using Olbrasoft.VoiceAssistant.PushToTalkDictation;
using Olbrasoft.VoiceAssistant.PushToTalkDictation.Service;
using Olbrasoft.VoiceAssistant.PushToTalkDictation.Service.Hubs;
using Olbrasoft.VoiceAssistant.PushToTalkDictation.Service.Services;
using Olbrasoft.VoiceAssistant.Shared.Speech;
using Olbrasoft.VoiceAssistant.Shared.TextInput;
using VoiceAssistant.Data.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Get configuration values
var keyboardDevice = builder.Configuration.GetValue<string?>("PushToTalkDictation:KeyboardDevice");
var ggmlModelPath = builder.Configuration.GetValue<string>("PushToTalkDictation:GgmlModelPath") 
    ?? Path.Combine(AppContext.BaseDirectory, "models", "ggml-medium.bin");
var whisperLanguage = builder.Configuration.GetValue<string>("PushToTalkDictation:WhisperLanguage") ?? "cs";

// SignalR
builder.Services.AddSignalR();

// CORS (pro webové klienty)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// PTT Notifier service
builder.Services.AddSingleton<IPttNotifier, PttNotifier>();

// Register services
builder.Services.AddSingleton<IKeyboardMonitor>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<EvdevKeyboardMonitor>>();
    return new EvdevKeyboardMonitor(logger, keyboardDevice);
});

builder.Services.AddSingleton<IAudioRecorder, AlsaAudioRecorder>();

builder.Services.AddSingleton<ISpeechTranscriber>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<WhisperNetTranscriber>>();
    return new WhisperNetTranscriber(logger, ggmlModelPath, whisperLanguage);
});

// Auto-detect display server (X11/Wayland) and use appropriate text typer
builder.Services.AddSingleton<ITextTyper>(sp =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    var logger = sp.GetRequiredService<ILogger<Program>>();
    var displayServer = TextTyperFactory.GetDisplayServerName();
    logger.LogInformation("Detected display server: {DisplayServer}", displayServer);
    return TextTyperFactory.Create(loggerFactory);
});

// HTTP client for TTS stop functionality
builder.Services.AddHttpClient<DictationWorker>();

// Register VoiceAssistant.Shared.Data (EF Core, Mediation handlers)
builder.Services.AddVoiceAssistantData(builder.Configuration);

// Register worker
builder.Services.AddHostedService<DictationWorker>();

// Configure logging
builder.Logging.AddConsole();
builder.Logging.AddSystemdConsole();

var app = builder.Build();

app.UseCors();

// Map SignalR hub
app.MapHub<PttHub>("/hubs/ptt");

// Health check endpoint
app.MapGet("/", () => Results.Ok(new { service = "PushToTalkDictation", status = "running" }));

// Run on port 5050
app.Run("http://localhost:5050");
