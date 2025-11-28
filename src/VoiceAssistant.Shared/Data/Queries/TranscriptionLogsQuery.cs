using Olbrasoft.Data.Cqrs;
using Olbrasoft.Mediation;
using VoiceAssistant.Shared.Data.Dtos;
using VoiceAssistant.Shared.Data.Enums;

namespace VoiceAssistant.Shared.Data.Queries;

/// <summary>
/// Query to get recent transcription logs.
/// </summary>
public class TranscriptionLogsQuery : BaseQuery<IEnumerable<TranscriptionLogDto>>
{
    /// <summary>
    /// Gets or sets the maximum number of results to return.
    /// </summary>
    public int Take { get; set; } = 100;

    /// <summary>
    /// Gets or sets the source filter (optional).
    /// </summary>
    public TranscriptionSource? Source { get; set; }

    /// <summary>
    /// Gets or sets the start date filter (optional).
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Gets or sets the end date filter (optional).
    /// </summary>
    public DateTime? ToDate { get; set; }

    public TranscriptionLogsQuery(IQueryProcessor processor) : base(processor)
    {
    }

    public TranscriptionLogsQuery(IMediator mediator) : base(mediator)
    {
    }
    
    public TranscriptionLogsQuery()
    {
    }
}
