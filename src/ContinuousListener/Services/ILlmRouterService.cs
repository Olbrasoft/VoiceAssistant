using VoiceAssistant.Shared.Data.Enums;

namespace Olbrasoft.VoiceAssistant.ContinuousListener.Services;

/// <summary>
/// Interface for LLM-based voice input routing services.
/// Implementations determine whether input should be sent to OpenCode, responded directly, 
/// executed as bash command, or ignored.
/// </summary>
public interface ILlmRouterService
{
    /// <summary>
    /// Name of the LLM provider (for logging).
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Routes voice input through the LLM and returns the routing decision.
    /// </summary>
    /// <param name="inputText">The transcribed voice input.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Routing result with action, response, and timing information.</returns>
    Task<LlmRouterResult> RouteAsync(string inputText, CancellationToken cancellationToken = default);
}

/// <summary>
/// Possible actions from LLM routing.
/// </summary>
public enum LlmRouterAction
{
    /// <summary>Send command to OpenCode.</summary>
    OpenCode,
    /// <summary>Respond directly via TTS.</summary>
    Respond,
    /// <summary>Execute bash command.</summary>
    Bash,
    /// <summary>Ignore the input.</summary>
    Ignore
}

/// <summary>
/// Result from LLM Router.
/// </summary>
public class LlmRouterResult
{
    public bool Success { get; init; }
    public LlmRouterAction Action { get; init; }
    public float Confidence { get; init; }
    public string? Reason { get; init; }
    public string? Response { get; init; }
    public string? CommandForOpenCode { get; init; }
    public string? BashCommand { get; init; }
    public int ResponseTimeMs { get; init; }
    public string? ErrorMessage { get; init; }

    public static LlmRouterResult Ignored(string reason) => new()
    {
        Success = true,
        Action = LlmRouterAction.Ignore,
        Confidence = 1.0f,
        Reason = reason,
        ResponseTimeMs = 0
    };

    public static LlmRouterResult Error(string message, int responseTimeMs) => new()
    {
        Success = false,
        Action = LlmRouterAction.Ignore,
        ErrorMessage = message,
        ResponseTimeMs = responseTimeMs
    };
}
