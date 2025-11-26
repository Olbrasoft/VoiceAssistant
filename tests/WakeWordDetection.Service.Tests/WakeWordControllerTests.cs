using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Olbrasoft.VoiceAssistant.WakeWordDetection.Service.Controllers;
using Xunit;

namespace Olbrasoft.VoiceAssistant.WakeWordDetection.Service.Tests;

/// <summary>
/// Unit tests for WakeWordController class.
/// </summary>
public class WakeWordControllerTests
{
    private readonly Mock<IWakeWordDetector> _detectorMock;
    private readonly Mock<ILogger<WakeWordController>> _loggerMock;

    public WakeWordControllerTests()
    {
        _detectorMock = new Mock<IWakeWordDetector>();
        _loggerMock = new Mock<ILogger<WakeWordController>>();
    }

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Act
        var controller = new WakeWordController(_detectorMock.Object, _loggerMock.Object);

        // Assert
        Assert.NotNull(controller);
    }

    [Fact]
    public void GetStatus_ShouldReturnStatusWithIsListening()
    {
        // Arrange
        _detectorMock.Setup(x => x.IsListening).Returns(true);
        var controller = new WakeWordController(_detectorMock.Object, _loggerMock.Object);

        // Act
        var result = controller.GetStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        var statusObj = okResult.Value;
        var isListeningProp = statusObj!.GetType().GetProperty("IsListening");
        Assert.NotNull(isListeningProp);
        Assert.True((bool)isListeningProp.GetValue(statusObj)!);
    }

    [Fact]
    public void GetStatus_WhenNotListening_ShouldReturnFalse()
    {
        // Arrange
        _detectorMock.Setup(x => x.IsListening).Returns(false);
        var controller = new WakeWordController(_detectorMock.Object, _loggerMock.Object);

        // Act
        var result = controller.GetStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var statusObj = okResult.Value;
        var isListeningProp = statusObj!.GetType().GetProperty("IsListening");
        Assert.False((bool)isListeningProp!.GetValue(statusObj)!);
    }

    [Fact]
    public void GetConfiguredWords_ShouldReturnWords()
    {
        // Arrange
        var words = new List<string> { "jarvis", "alexa" }.AsReadOnly();
        _detectorMock.Setup(x => x.GetWakeWords()).Returns(words);
        var controller = new WakeWordController(_detectorMock.Object, _loggerMock.Object);

        // Act
        var result = controller.GetConfiguredWords();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var resultObj = okResult.Value;
        var wordsProp = resultObj!.GetType().GetProperty("Words");
        var returnedWords = (IReadOnlyCollection<string>)wordsProp!.GetValue(resultObj)!;
        Assert.Equal(2, returnedWords.Count);
        Assert.Contains("jarvis", returnedWords);
        Assert.Contains("alexa", returnedWords);
    }

    [Fact]
    public void GetInfo_ShouldReturnServiceInformation()
    {
        // Arrange
        var words = new List<string> { "jarvis" }.AsReadOnly();
        _detectorMock.Setup(x => x.GetWakeWords()).Returns(words);
        var controller = new WakeWordController(_detectorMock.Object, _loggerMock.Object);

        // Act
        var result = controller.GetInfo();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var infoObj = okResult.Value;
        
        var serviceNameProp = infoObj!.GetType().GetProperty("ServiceName");
        Assert.Equal("WakeWord Listener", serviceNameProp!.GetValue(infoObj));
        
        var endpointProp = infoObj.GetType().GetProperty("WebSocketEndpoint");
        Assert.Equal("/hubs/wakeword", endpointProp!.GetValue(infoObj));
    }

    [Fact]
    public void TriggerDetection_WithOpenWakeWordDetector_ShouldReturnBadRequest()
    {
        // Arrange - OpenWakeWord doesn't support manual trigger
        var controller = new WakeWordController(_detectorMock.Object, _loggerMock.Object);

        // Act
        var result = controller.TriggerDetection("jarvis");

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public void TriggerDetection_WithNonPorcupineDetector_ShouldReturnBadRequest()
    {
        // Arrange
        var controller = new WakeWordController(_detectorMock.Object, _loggerMock.Object);

        // Act
        var result = controller.TriggerDetection("jarvis");

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var messageObj = badRequestResult.Value;
        var messageProp = messageObj!.GetType().GetProperty("Message");
        Assert.Contains("not supported", messageProp!.GetValue(messageObj)?.ToString() ?? "");
    }
}
