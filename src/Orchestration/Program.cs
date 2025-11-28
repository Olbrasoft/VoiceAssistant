using Microsoft.AspNetCore.Builder;
using Olbrasoft.VoiceAssistant.Orchestration;
using Olbrasoft.VoiceAssistant.Orchestration.Services;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddSingleton<SpeechRecognitionService>();
builder.Services.AddSingleton<TextInputService>();
builder.Services.AddSingleton<IOrchestrator, Orchestrator>();
builder.Services.AddHostedService<Worker>();

// Add Web API support
builder.Services.AddControllers();

var app = builder.Build();

// Configure HTTP pipeline
app.MapControllers();

app.Run();
