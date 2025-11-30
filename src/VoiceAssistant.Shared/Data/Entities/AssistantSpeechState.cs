using Olbrasoft.Data.Entities.Abstractions;

namespace VoiceAssistant.Shared.Data.Entities;

/// <summary>
/// Entity representing the current speech state of the assistant.
/// Used to track when the assistant is speaking so that the listener
/// doesn't lock TTS when it hears the assistant's own voice.
/// </summary>
public class AssistantSpeechState : BaseEnity
{
    /// <summary>
    /// Gets or sets whether the assistant is currently speaking.
    /// </summary>
    public bool IsSpeaking { get; set; }

    /// <summary>
    /// Gets or sets when the assistant started speaking (null if not speaking).
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets when the assistant stopped speaking (null if still speaking).
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
