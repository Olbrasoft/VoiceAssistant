using Microsoft.EntityFrameworkCore;
using Olbrasoft.Text.Similarity;
using Olbrasoft.VoiceAssistant.ContinuousListener;
using Olbrasoft.VoiceAssistant.ContinuousListener.Services;
using VoiceAssistant.Data.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on specific port
var listenerPort = builder.Configuration.GetValue<int>("ListenerApiPort", 5051);
builder.WebHost.UseUrls($"http://localhost:{listenerPort}");

// Configure database
var dbPath = builder.Configuration.GetValue<string>("DatabasePath") 
    ?? "/home/jirka/voice-assistant/voice-assistant.db";
builder.Services.AddDbContext<VoiceAssistantDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Configure HTTP clients - disable cookies to avoid rate limit issues
// Use Cerebras if configured, otherwise fall back to Groq
var useCerebras = !string.IsNullOrEmpty(builder.Configuration["CerebrasRouter:ApiKey"]);

if (useCerebras)
{
    builder.Services.AddHttpClient<CerebrasRouterService>()
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            UseCookies = false,
            AutomaticDecompression = System.Net.DecompressionMethods.All
        });
    builder.Services.AddSingleton<ILlmRouterService, CerebrasRouterService>();
}
else
{
    builder.Services.AddHttpClient<GroqRouterService>()
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            UseCookies = false,
            AutomaticDecompression = System.Net.DecompressionMethods.All
        });
    builder.Services.AddSingleton<ILlmRouterService, GroqRouterService>();
}

// Configure services
builder.Services.AddSingleton<IStringSimilarity, LevenshteinSimilarity>();
builder.Services.AddSingleton<AudioCaptureService>();
builder.Services.AddSingleton<VadService>();
builder.Services.AddSingleton<TranscriptionService>();
builder.Services.AddSingleton<CommandDispatcher>();
builder.Services.AddSingleton<TtsControlService>();
builder.Services.AddSingleton<TtsPlaybackService>();
builder.Services.AddSingleton<SpeechLockService>();
builder.Services.AddSingleton<AssistantSpeechStateService>();
// BashExecutionService removed - all bash commands now routed to OpenCode (issue #5)
builder.Services.AddSingleton<AssistantSpeechTrackerService>();

// Main worker
builder.Services.AddHostedService<ContinuousListenerWorker>();

var app = builder.Build();

// API endpoints for assistant speech tracking
var speechTracker = app.Services.GetRequiredService<AssistantSpeechTrackerService>();

// Called by EdgeTTS when it starts speaking
app.MapPost("/api/assistant-speech/start", (AssistantSpeechStartRequest request) =>
{
    // Zelený výpis textu, který se chystám říct
    Console.WriteLine($"\u001b[92;1m🗣️ TTS: \"{request.Text}\"\u001b[0m");
    speechTracker.StartSpeaking(request.Text);
    return Results.Ok(new { status = "started", text = request.Text });
});

// Called by EdgeTTS when it stops speaking
app.MapPost("/api/assistant-speech/end", () =>
{
    speechTracker.StopSpeaking();
    return Results.Ok(new { status = "ended" });
});

// Status endpoint for debugging
app.MapGet("/api/assistant-speech/status", () =>
{
    return Results.Ok(new 
    { 
        historyCount = speechTracker.GetHistoryCount()
    });
});

// Health check
app.MapGet("/health", () => Results.Ok("OK"));

app.Run();

// Request DTOs
public record AssistantSpeechStartRequest(string Text);
