using Olbrasoft.VoiceAssistant.WakeWordDetection;
using Olbrasoft.VoiceAssistant.WakeWordDetection.Service;
using Olbrasoft.VoiceAssistant.WakeWordDetection.Service.Hubs;
using Olbrasoft.VoiceAssistant.WakeWordDetection.Service.Services;

var builder = WebApplication.CreateBuilder(args);

// Worker Service
builder.Services.AddHostedService<WakeWordWorker>();

// SignalR
builder.Services.AddSignalR();

// API Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

// Služby
builder.Services.AddSingleton<IEventBroadcastService, EventBroadcastService>();
builder.Services.AddSingleton<IAudioCapture>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var audioDeviceNumber = configuration.GetValue<int>("WakeWord:AudioDeviceNumber", 2); // TONOR TC30
    
    return new AlsaAudioCapture(
        sampleRate: 16000,
        channels: 1,
        bitsPerSample: 16,
        deviceNumber: audioDeviceNumber);
});

builder.Services.AddSingleton<IWakeWordDetector>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<OpenWakeWordDetector>>();
    var audioCapture = sp.GetRequiredService<IAudioCapture>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    
    // Use OpenWakeWord for wake word detection
    var modelsPath = Path.Combine(AppContext.BaseDirectory, "Models");
    
    // Load all ONNX wake word models (except melspec and embedding)
    // Supports both old format (*_v0.1.onnx) and new format with threshold (*_v0.1_t0.X.onnx)
    var wakeWordModels = Directory.GetFiles(modelsPath, "*_v0.1*.onnx")
        .Where(f => !f.Contains("melspectrogram") && !f.Contains("embedding"))
        .ToList();
    
    var melspecModelPath = Path.Combine(modelsPath, "melspectrogram.onnx");
    var embeddingModelPath = Path.Combine(modelsPath, "embedding_model.onnx");
    
    var defaultThreshold = configuration.GetValue<float>("WakeWord:DefaultThreshold", 0.5f);
    var debounceSeconds = configuration.GetValue<double>("WakeWord:DebounceSeconds", 2.0);
    
    // Create model provider service
    var modelProvider = new FileBasedModelProvider(defaultThreshold);
    
    logger.LogInformation("Initializing OpenWakeWordDetector");
    logger.LogInformation("  Models path: {ModelsPath}", modelsPath);
    logger.LogInformation("  Loading {Count} wake word models", wakeWordModels.Count);
    foreach (var model in wakeWordModels)
    {
        logger.LogInformation("    - {Model}", Path.GetFileName(model));
    }
    logger.LogInformation("  Default Threshold: {DefaultThreshold}", defaultThreshold);
    logger.LogInformation("  Debounce: {Debounce}s", debounceSeconds);
    
    return new OpenWakeWordDetector(
        logger,
        audioCapture,
        modelProvider,
        wakeWordModels,
        melspecModelPath,
        embeddingModelPath,
        debounceSeconds);
});

var app = builder.Build();

// Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.MapControllers();
app.MapHub<WakeWordHub>("/hubs/wakeword");

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run("http://localhost:5000");
