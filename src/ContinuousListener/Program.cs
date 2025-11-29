using Microsoft.EntityFrameworkCore;
using Olbrasoft.VoiceAssistant.ContinuousListener;
using Olbrasoft.VoiceAssistant.ContinuousListener.Services;
using VoiceAssistant.Data.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// Configure database
var dbPath = builder.Configuration.GetValue<string>("DatabasePath") 
    ?? "/home/jirka/voice-assistant/voice-assistant.db";
builder.Services.AddDbContext<VoiceAssistantDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Configure services
builder.Services.AddSingleton<AudioCaptureService>();
builder.Services.AddSingleton<VadService>();
builder.Services.AddSingleton<TranscriptionService>();
builder.Services.AddSingleton<WakeWordService>();
builder.Services.AddSingleton<CommandDispatcher>();
builder.Services.AddSingleton<WakeWordResponseService>();
builder.Services.AddSingleton<TtsControlService>();
builder.Services.AddSingleton<SpeechLockService>();

// Main worker
builder.Services.AddHostedService<ContinuousListenerWorker>();

var host = builder.Build();
host.Run();
