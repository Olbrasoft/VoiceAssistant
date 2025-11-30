using Olbrasoft.Data.Cqrs;
using Olbrasoft.Mediation;
using VoiceAssistant.Shared.Data.Enums;

namespace VoiceAssistant.Shared.Data.Commands.SpeechLockCommands;

/// <summary>
/// Command to create a speech lock (when user starts recording).
/// Returns the ID of the created lock.
/// </summary>
public class SpeechLockCreateCommand : VoiceAssistantCommand<int>
{
    /// <summary>
    /// Source of the speech lock (who is requesting the lock).
    /// </summary>
    public SpeechLockSource Source { get; set; } = SpeechLockSource.ContinuousListener;

    /// <summary>
    /// Optional reason for the lock (e.g., "Recording", "WakeWord:OpenCode").
    /// </summary>
    public string? Reason { get; set; }

    public SpeechLockCreateCommand(ICommandExecutor executor) : base(executor)
    {
    }

    public SpeechLockCreateCommand(IMediator mediator) : base(mediator)
    {
    }

    public SpeechLockCreateCommand()
    {
    }
}
