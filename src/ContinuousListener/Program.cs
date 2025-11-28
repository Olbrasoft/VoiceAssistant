using Olbrasoft.VoiceAssistant.ContinuousListener;
using Olbrasoft.VoiceAssistant.ContinuousListener.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configure services
builder.Services.AddSingleton<AudioCaptureService>();
builder.Services.AddSingleton<VadService>();
builder.Services.AddSingleton<TranscriptionService>();
builder.Services.AddSingleton<WakeWordService>();
builder.Services.AddSingleton<CommandDispatcher>();

// Main worker
builder.Services.AddHostedService<ContinuousListenerWorker>();

var host = builder.Build();
host.Run();
