using Microsoft.AspNetCore.Mvc;
using Olbrasoft.VoiceAssistant.Orchestration;

namespace Olbrasoft.VoiceAssistant.Orchestration.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VoiceController : ControllerBase
{
    private readonly IOrchestrator _orchestrator;
    private readonly ILogger<VoiceController> _logger;

    public VoiceController(IOrchestrator orchestrator, ILogger<VoiceController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    [HttpPost("dictate")]
    public async Task<IActionResult> StartDictation([FromQuery] bool? submit = null)
    {
        try
        {
            _logger.LogInformation("API: Manual dictation triggered (submit: {Submit})", submit?.ToString() ?? "default");
            await _orchestrator.TriggerDictationAsync();
            return Ok(new { success = true, message = "Dictation started", submit = submit ?? true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API: Error starting dictation");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new 
        { 
            success = true, 
            service = "Orchestration Voice Assistant",
            status = "running",
            timestamp = DateTime.UtcNow
        });
    }
}
