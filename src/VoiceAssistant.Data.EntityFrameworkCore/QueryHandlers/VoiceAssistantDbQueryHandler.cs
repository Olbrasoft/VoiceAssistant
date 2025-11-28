using Olbrasoft.Data.Cqrs;
using Olbrasoft.Data.Cqrs.EntityFrameworkCore;

namespace VoiceAssistant.Data.EntityFrameworkCore.QueryHandlers;

/// <summary>
/// Base class for VoiceAssistant query handlers with custom result type.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
/// <typeparam name="TQuery">The type of the query.</typeparam>
/// <typeparam name="TResult">The type of the result.</typeparam>
public abstract class VoiceAssistantDbQueryHandler<TEntity, TQuery, TResult> 
    : DbQueryHandler<VoiceAssistantDbContext, TEntity, TQuery, TResult> 
    where TQuery : BaseQuery<TResult> 
    where TEntity : class
{
    protected VoiceAssistantDbQueryHandler(VoiceAssistantDbContext context) : base(context)
    {
    }

    public override Task<TResult> HandleAsync(TQuery query, CancellationToken token)
    {
        ThrowIfQueryIsNullOrCancellationRequested(query, token);
        return GetResultToHandleAsync(query, token);
    }

    protected abstract Task<TResult> GetResultToHandleAsync(TQuery query, CancellationToken token);
}

/// <summary>
/// Base class for VoiceAssistant query handlers returning bool.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
/// <typeparam name="TQuery">The type of the query.</typeparam>
public abstract class VoiceAssistantDbQueryHandler<TEntity, TQuery> 
    : VoiceAssistantDbQueryHandler<TEntity, TQuery, bool> 
    where TQuery : BaseQuery<bool> 
    where TEntity : class
{
    protected VoiceAssistantDbQueryHandler(VoiceAssistantDbContext context) : base(context)
    {
    }
}
