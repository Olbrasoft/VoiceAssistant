using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Olbrasoft.VoiceAssistant.Orchestration.Controllers;

namespace Olbrasoft.VoiceAssistant.Orchestration.Tests.Controllers;

public class VoiceControllerTests
{
    private readonly Mock<IOrchestrator> _mockOrchestrator;
    private readonly Mock<ILogger<VoiceController>> _mockLogger;
    private readonly VoiceController _controller;

    public VoiceControllerTests()
    {
        _mockOrchestrator = new Mock<IOrchestrator>();
        _mockLogger = new Mock<ILogger<VoiceController>>();
        _controller = new VoiceController(_mockOrchestrator.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task StartDictation_WhenSuccessful_ShouldReturnOkResult()
    {
        // Arrange
        _mockOrchestrator
            .Setup(o => o.TriggerDictationAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.StartDictation();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task StartDictation_WhenSuccessful_ShouldCallTriggerDictationAsync()
    {
        // Arrange
        _mockOrchestrator
            .Setup(o => o.TriggerDictationAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _controller.StartDictation();

        // Assert
        _mockOrchestrator.Verify(o => o.TriggerDictationAsync(), Times.Once);
    }

    [Fact]
    public async Task StartDictation_WhenExceptionThrown_ShouldReturn500()
    {
        // Arrange
        _mockOrchestrator
            .Setup(o => o.TriggerDictationAsync())
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.StartDictation();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task StartDictation_WithSubmitParameter_ShouldPassSubmitValue()
    {
        // Arrange
        _mockOrchestrator
            .Setup(o => o.TriggerDictationAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.StartDictation(submit: false);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public void GetStatus_ShouldReturnOkResult()
    {
        // Act
        var result = _controller.GetStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public void Controller_ShouldHaveApiControllerAttribute()
    {
        // Arrange & Act
        var attributes = typeof(VoiceController).GetCustomAttributes(typeof(ApiControllerAttribute), true);

        // Assert
        Assert.Single(attributes);
    }

    [Fact]
    public void Controller_ShouldHaveRouteAttribute()
    {
        // Arrange & Act
        var attributes = typeof(VoiceController).GetCustomAttributes(typeof(RouteAttribute), true);

        // Assert
        Assert.Single(attributes);
        var routeAttribute = (RouteAttribute)attributes[0];
        Assert.Equal("api/[controller]", routeAttribute.Template);
    }

    [Fact]
    public void StartDictation_ShouldHaveHttpPostAttribute()
    {
        // Arrange
        var method = typeof(VoiceController).GetMethod(nameof(VoiceController.StartDictation));

        // Act
        var attributes = method!.GetCustomAttributes(typeof(HttpPostAttribute), true);

        // Assert
        Assert.Single(attributes);
        var httpPostAttribute = (HttpPostAttribute)attributes[0];
        Assert.Equal("dictate", httpPostAttribute.Template);
    }

    [Fact]
    public void GetStatus_ShouldHaveHttpGetAttribute()
    {
        // Arrange
        var method = typeof(VoiceController).GetMethod(nameof(VoiceController.GetStatus));

        // Act
        var attributes = method!.GetCustomAttributes(typeof(HttpGetAttribute), true);

        // Assert
        Assert.Single(attributes);
        var httpGetAttribute = (HttpGetAttribute)attributes[0];
        Assert.Equal("status", httpGetAttribute.Template);
    }
}
