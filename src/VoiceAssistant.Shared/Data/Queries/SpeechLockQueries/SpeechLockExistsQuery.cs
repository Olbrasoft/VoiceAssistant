using Olbrasoft.Data.Cqrs;
using Olbrasoft.Mediation;

namespace VoiceAssistant.Shared.Data.Queries.SpeechLockQueries;

/// <summary>
/// Query to check if an active speech lock exists (created within the last 5 minutes).
/// Returns true if AI should NOT speak.
/// </summary>
public class SpeechLockExistsQuery : BaseQuery<bool>
{
    /// <summary>
    /// Gets or sets the maximum age in minutes for a lock to be considered active.
    /// Default is 5 minutes.
    /// </summary>
    public int MaxAgeMinutes { get; set; } = 5;

    public SpeechLockExistsQuery(IQueryProcessor processor) : base(processor)
    {
    }

    public SpeechLockExistsQuery(IMediator mediator) : base(mediator)
    {
    }

    public SpeechLockExistsQuery()
    {
    }
}
