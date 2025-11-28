using Olbrasoft.VoiceAssistant.PushToTalkDictation;
using Olbrasoft.VoiceAssistant.PushToTalkDictation.Service;
using Olbrasoft.VoiceAssistant.Shared.Speech;
using Olbrasoft.VoiceAssistant.Shared.TextInput;

var builder = Host.CreateApplicationBuilder(args);

// Get configuration values
var keyboardDevice = builder.Configuration.GetValue<string?>("PushToTalkDictation:KeyboardDevice");
var ggmlModelPath = builder.Configuration.GetValue<string>("PushToTalkDictation:GgmlModelPath") 
    ?? Path.Combine(AppContext.BaseDirectory, "models", "ggml-medium.bin");
var whisperLanguage = builder.Configuration.GetValue<string>("PushToTalkDictation:WhisperLanguage") ?? "cs";

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

// Register worker
builder.Services.AddHostedService<DictationWorker>();

// Configure logging
builder.Logging.AddConsole();
builder.Logging.AddSystemdConsole();

var host = builder.Build();

host.Run();
