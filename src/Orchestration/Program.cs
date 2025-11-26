using Olbrasoft.VoiceAssistant.Orchestration;
using Olbrasoft.VoiceAssistant.Orchestration.Services;

var builder = Host.CreateApplicationBuilder(args);

// Register services
builder.Services.AddSingleton<AudioResponsePlayer>();
builder.Services.AddSingleton<SpeechRecognitionService>();
builder.Services.AddSingleton<TextInputService>();
builder.Services.AddSingleton<IOrchestrator, Orchestrator>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
