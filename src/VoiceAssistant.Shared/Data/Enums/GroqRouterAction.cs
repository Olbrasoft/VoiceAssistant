using System.ComponentModel;

namespace VoiceAssistant.Shared.Data.Enums;

/// <summary>
/// Represents the action determined by Groq Router for a voice input.
/// </summary>
public enum GroqRouterAction
{
    /// <summary>
    /// Route the command to OpenCode for processing.
    /// </summary>
    [Description("Route to OpenCode for programming tasks")]
    OpenCode = 1,

    /// <summary>
    /// Groq responded directly (e.g., time, date, simple questions).
    /// </summary>
    [Description("Direct response from Groq (TTS playback)")]
    Respond = 2,

    /// <summary>
    /// Ignore the input (random conversation, noise, etc.).
    /// </summary>
    [Description("Ignored input (not relevant)")]
    Ignore = 3,

    /// <summary>
    /// Execute a bash command on the system.
    /// </summary>
    [Description("Execute bash command")]
    Bash = 4
}
