using Olbrasoft.Data.Cqrs;
using Olbrasoft.Mediation;

namespace VoiceAssistant.Shared.Data.Commands.SpeechLockCommands;

/// <summary>
/// Command to delete a speech lock (when user stops recording).
/// Returns true if lock was deleted.
/// </summary>
public class SpeechLockDeleteCommand : VoiceAssistantCommand
{
    /// <summary>
    /// Optional: specific lock ID to delete. If not set (0), all locks are deleted.
    /// </summary>
    public new int Id { get; set; }

    public SpeechLockDeleteCommand(ICommandExecutor executor) : base(executor)
    {
    }

    public SpeechLockDeleteCommand(IMediator mediator) : base(mediator)
    {
    }

    public SpeechLockDeleteCommand()
    {
    }
}
