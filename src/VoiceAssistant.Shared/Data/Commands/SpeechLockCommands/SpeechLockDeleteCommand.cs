using Olbrasoft.Data.Cqrs;
using Olbrasoft.Mediation;

namespace VoiceAssistant.Shared.Data.Commands.SpeechLockCommands;

/// <summary>
/// Command to delete a speech lock (when user stops recording).
/// Returns true if lock was deleted.
/// </summary>
public class SpeechLockDeleteCommand : VoiceAssistantCommand
{
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
