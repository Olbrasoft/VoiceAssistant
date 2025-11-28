using Olbrasoft.Data.Cqrs;
using Olbrasoft.Mediation;

namespace VoiceAssistant.Shared.Data.Commands;

/// <summary>
/// Base command for VoiceAssistant returning bool result.
/// </summary>
public abstract class VoiceAssistantCommand : VoiceAssistantCommand<bool>
{
    protected VoiceAssistantCommand(ICommandExecutor executor) : base(executor)
    {
    }

    protected VoiceAssistantCommand(IMediator mediator) : base(mediator)
    {
    }
    
    protected VoiceAssistantCommand()
    {
    }
}

/// <summary>
/// Base command for VoiceAssistant with custom result type.
/// </summary>
/// <typeparam name="TResult">The type of the command result.</typeparam>
public abstract class VoiceAssistantCommand<TResult> : BaseCommand<TResult>
{
    /// <summary>
    /// Gets or sets the entity identifier.
    /// </summary>
    public int Id { get; set; }

    protected VoiceAssistantCommand(ICommandExecutor executor) : base(executor)
    {
    }

    protected VoiceAssistantCommand(IMediator mediator) : base(mediator)
    {
    }
    
    protected VoiceAssistantCommand()
    {
    }
}
