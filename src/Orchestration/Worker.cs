namespace Olbrasoft.VoiceAssistant.Orchestration;

/// <summary>
/// Background worker service that runs the voice assistant orchestrator.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IOrchestrator _orchestrator;
    
    public Worker(ILogger<Worker> logger, IOrchestrator orchestrator)
    {
        _logger = logger;
        _orchestrator = orchestrator;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Voice Assistant Orchestrator starting at: {Time}", DateTimeOffset.Now);
        
        try
        {
            await _orchestrator.StartAsync(stoppingToken);
            
            _logger.LogInformation("Voice Assistant Orchestrator is running");
            
            // Keep running until cancellation is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Voice Assistant Orchestrator is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Voice Assistant Orchestrator");
            throw;
        }
        finally
        {
            await _orchestrator.StopAsync(CancellationToken.None);
        }
    }
}
