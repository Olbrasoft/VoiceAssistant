using Microsoft.EntityFrameworkCore;
using Olbrasoft.Data.Cqrs;
using Olbrasoft.Data.Cqrs.EntityFrameworkCore;
using Olbrasoft.Mapping;
using VoiceAssistant.Shared.Data.Commands;
using VoiceAssistant.Shared.Data.Entities;

namespace VoiceAssistant.Data.EntityFrameworkCore.CommandHandlers;

/// <summary>
/// Base class for VoiceAssistant command handlers.
/// </summary>
/// <typeparam name="TCommand">The type of the command.</typeparam>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
public abstract class VoiceAssistantDbCommandHandler<TCommand, TEntity> 
    : DbBaseCommandHandler<VoiceAssistantDbContext, TEntity, TCommand, bool>
    where TCommand : ICommand<bool> 
    where TEntity : Olbrasoft.Data.Entities.Abstractions.BaseEnity
{
    protected VoiceAssistantDbCommandHandler(VoiceAssistantDbContext context) : base(context)
    {
    }

    protected VoiceAssistantDbCommandHandler(IProjector projector, VoiceAssistantDbContext context) 
        : base(projector, context)
    {
    }

    protected VoiceAssistantDbCommandHandler(IMapper mapper, VoiceAssistantDbContext context) 
        : base(mapper, context)
    {
    }

    public override Task<bool> HandleAsync(TCommand command, CancellationToken token)
    {
        ThrowIfCommandIsNullOrCancellationRequested(command, token);
        return GetResultToHandleAsync(command, token);
    }

    protected abstract Task<bool> GetResultToHandleAsync(TCommand command, CancellationToken token);
}

/// <summary>
/// Base class for VoiceAssistant command handlers with custom result type.
/// </summary>
/// <typeparam name="TCommand">The type of the command.</typeparam>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
/// <typeparam name="TResult">The type of the result.</typeparam>
public abstract class VoiceAssistantDbCommandHandler<TCommand, TEntity, TResult> 
    : DbBaseCommandHandler<VoiceAssistantDbContext, TEntity, TCommand, TResult>
    where TCommand : ICommand<TResult> 
    where TEntity : Olbrasoft.Data.Entities.Abstractions.BaseEnity
{
    protected VoiceAssistantDbCommandHandler(VoiceAssistantDbContext context) : base(context)
    {
    }

    protected VoiceAssistantDbCommandHandler(IProjector projector, VoiceAssistantDbContext context) 
        : base(projector, context)
    {
    }

    protected VoiceAssistantDbCommandHandler(IMapper mapper, VoiceAssistantDbContext context) 
        : base(mapper, context)
    {
    }

    public override Task<TResult> HandleAsync(TCommand command, CancellationToken token)
    {
        ThrowIfCommandIsNullOrCancellationRequested(command, token);
        return GetResultToHandleAsync(command, token);
    }

    protected abstract Task<TResult> GetResultToHandleAsync(TCommand command, CancellationToken token);
}
