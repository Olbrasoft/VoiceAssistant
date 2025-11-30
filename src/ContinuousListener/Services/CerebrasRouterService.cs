namespace Olbrasoft.VoiceAssistant.ContinuousListener.Services;

/// <summary>
/// Cerebras LLM router service implementation.
/// Uses Cerebras Inference API (OpenAI-compatible).
/// </summary>
public class CerebrasRouterService : BaseLlmRouterService
{
    public override string ProviderName => "Cerebras";

    public CerebrasRouterService(
        ILogger<CerebrasRouterService> logger,
        HttpClient httpClient,
        IConfiguration configuration)
        : base(logger, httpClient, GetModel(configuration))
    {
        var apiKey = configuration["CerebrasRouter:ApiKey"] ?? "";
        
        httpClient.BaseAddress = new Uri("https://api.cerebras.ai/v1/");
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        httpClient.Timeout = TimeSpan.FromSeconds(30);
        
        logger.LogInformation("Cerebras Router initialized with model {Model}", _model);
    }

    private static string GetModel(IConfiguration configuration)
    {
        return configuration["CerebrasRouter:Model"] ?? "llama-3.3-70b";
    }
}
