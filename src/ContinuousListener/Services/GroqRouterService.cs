namespace Olbrasoft.VoiceAssistant.ContinuousListener.Services;

/// <summary>
/// Groq LLM router service implementation.
/// Uses Groq API (OpenAI-compatible).
/// </summary>
public class GroqRouterService : BaseLlmRouterService
{
    public override string ProviderName => "Groq";

    public GroqRouterService(
        ILogger<GroqRouterService> logger,
        HttpClient httpClient,
        IConfiguration configuration)
        : base(logger, httpClient, GetModel(configuration))
    {
        var apiKey = configuration["GroqRouter:ApiKey"] ?? "";
        
        httpClient.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        httpClient.Timeout = TimeSpan.FromSeconds(30);
        
        logger.LogInformation("Groq Router initialized with model {Model}", _model);
    }

    private static string GetModel(IConfiguration configuration)
    {
        return configuration["GroqRouter:Model"] ?? "llama-3.3-70b-versatile";
    }
}
