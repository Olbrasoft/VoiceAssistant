using EdgeTtsWebSocketServer.Controllers;
using EdgeTtsWebSocketServer.Models;
using EdgeTtsWebSocketServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Olbrasoft.VoiceAssistant.EdgeTtsWebSocketServer.Tests.Controllers;

public class SpeechControllerTests
{
    private readonly Mock<EdgeTtsService> _mockEdgeTtsService;
    private readonly Mock<ILogger<SpeechController>> _mockLogger;
    private readonly SpeechController _controller;

    public SpeechControllerTests()
    {
        // EdgeTtsService requires IConfiguration and ILogger, so we need to mock those too
        var mockConfiguration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        var mockEdgeTtsLogger = new Mock<ILogger<EdgeTtsService>>();
        
        // Setup configuration defaults
        mockConfiguration.Setup(c => c["EdgeTts:CacheDirectory"]).Returns("/tmp/test-cache");
        mockConfiguration.Setup(c => c["EdgeTts:MicrophoneLockFile"]).Returns("/tmp/test-mic.lock");
        mockConfiguration.Setup(c => c["EdgeTts:SpeechLockFile"]).Returns("/tmp/test-speech.lock");
        mockConfiguration.Setup(c => c["EdgeTts:DefaultVoice"]).Returns("cs-CZ-AntoninNeural");
        mockConfiguration.Setup(c => c["EdgeTts:DefaultRate"]).Returns("+20%");

        _mockEdgeTtsService = new Mock<EdgeTtsService>(mockConfiguration.Object, mockEdgeTtsLogger.Object);
        _mockLogger = new Mock<ILogger<SpeechController>>();
        _controller = new SpeechController(_mockEdgeTtsService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Speak_WithEmptyText_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new SpeechRequest { Text = "" };

        // Act
        var result = await _controller.Speak(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<SpeechResponse>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Text is required", response.Message);
    }

    [Fact]
    public async Task Speak_WithWhitespaceText_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new SpeechRequest { Text = "   " };

        // Act
        var result = await _controller.Speak(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<SpeechResponse>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Text is required", response.Message);
    }

    [Fact]
    public async Task Speak_WithNullText_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new SpeechRequest { Text = null! };

        // Act
        var result = await _controller.Speak(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<SpeechResponse>(badRequestResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public void Controller_ShouldHaveApiControllerAttribute()
    {
        // Arrange & Act
        var attributes = typeof(SpeechController).GetCustomAttributes(typeof(ApiControllerAttribute), true);

        // Assert
        Assert.Single(attributes);
    }

    [Fact]
    public void Controller_ShouldHaveRouteAttribute()
    {
        // Arrange & Act
        var attributes = typeof(SpeechController).GetCustomAttributes(typeof(RouteAttribute), true);

        // Assert
        Assert.Single(attributes);
        var routeAttribute = (RouteAttribute)attributes[0];
        Assert.Equal("api/[controller]", routeAttribute.Template);
    }

    [Fact]
    public void Speak_ShouldHaveHttpPostAttribute()
    {
        // Arrange
        var method = typeof(SpeechController).GetMethod(nameof(SpeechController.Speak));

        // Act
        var attributes = method!.GetCustomAttributes(typeof(HttpPostAttribute), true);

        // Assert
        Assert.Single(attributes);
        var httpPostAttribute = (HttpPostAttribute)attributes[0];
        Assert.Equal("speak", httpPostAttribute.Template);
    }

    [Fact]
    public void ClearCache_ShouldHaveHttpDeleteAttribute()
    {
        // Arrange
        var method = typeof(SpeechController).GetMethod(nameof(SpeechController.ClearCache));

        // Act
        var attributes = method!.GetCustomAttributes(typeof(HttpDeleteAttribute), true);

        // Assert
        Assert.Single(attributes);
        var httpDeleteAttribute = (HttpDeleteAttribute)attributes[0];
        Assert.Equal("cache", httpDeleteAttribute.Template);
    }
}
