using Olbrasoft.Data.Cqrs;
using Olbrasoft.Mediation;

namespace VoiceAssistant.Shared.Data.Commands.SpeechLockCommands;

/// <summary>
/// Command to create a speech lock (when user starts recording).
/// Returns the ID of the created lock.
/// </summary>
public class SpeechLockCreateCommand : VoiceAssistantCommand<int>
{
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
