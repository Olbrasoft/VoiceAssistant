using Microsoft.AspNetCore.Mvc;

namespace Olbrasoft.VoiceAssistant.WakeWordDetection.Service.Controllers;

/// <summary>
/// REST API controller for wake word listener service management and status.
/// Provides endpoints for service information, status checking, and manual testing.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WakeWordController : ControllerBase
{
    private readonly IWakeWordDetector _detector;
    private readonly ILogger<WakeWordController> _logger;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="WakeWordController"/> class.
    /// </summary>
    /// <param name="detector">Wake word detector instance.</param>
    /// <param name="logger">Logger for API operations.</param>
    public WakeWordController(
        IWakeWordDetector detector,
        ILogger<WakeWordController> logger)
    {
        _detector = detector;
        _logger = logger;
    }
    
    /// <summary>
    /// Gets the current status of the wake word listener service.
    /// </summary>
    /// <returns>Service status including listening state, version, and timestamp.</returns>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            IsListening = _detector.IsListening,
            ServiceVersion = "1.0.0",
            Timestamp = DateTime.UtcNow
        });
    }
    
    /// <summary>
    /// Gets the list of configured wake words that the service is listening for.
    /// </summary>
    /// <returns>Array of configured wake words.</returns>
    [HttpGet("words")]
    public IActionResult GetConfiguredWords()
    {
        var words = _detector.GetWakeWords();
        return Ok(new { Words = words });
    }
    
    /// <summary>
    /// Gets comprehensive service information including endpoints and supported words.
    /// </summary>
    /// <returns>Service information object.</returns>
    [HttpGet("info")]
    public IActionResult GetInfo()
    {
        return Ok(new
        {
            ServiceName = "WakeWord Listener",
            Version = "1.0.0",
            WebSocketEndpoint = "/hubs/wakeword",
            SupportedWords = _detector.GetWakeWords()
        });
    }
    
    /// <summary>
    /// Manually triggers a wake word detection event for testing purposes.
    /// </summary>
    /// <param name="word">The wake word to trigger (default: "jarvis").</param>
    /// <returns>Result indicating whether the trigger was successful.</returns>
    [HttpPost("trigger")]
    public IActionResult TriggerDetection([FromQuery] string word = "jarvis")
    {
        _logger.LogInformation("Manual trigger requested for word: {Word}", word);
        
        // Manual trigger not supported for OpenWakeWord detector
        // (OpenWakeWord requires real audio processing through the pipeline)
        
        return BadRequest(new { Message = "Manual trigger not supported for OpenWakeWord detector. Use real audio input instead." });
    }
}
