using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Olbrasoft.VoiceAssistant.WakeWordDetection.Service.Hubs;
using Olbrasoft.VoiceAssistant.WakeWordDetection.Service.Models;
using Olbrasoft.VoiceAssistant.WakeWordDetection.Service.Services;
using Xunit;

namespace Olbrasoft.VoiceAssistant.WakeWordDetection.Service.Tests;

/// <summary>
/// Unit tests for EventBroadcastService class.
/// </summary>
public class EventBroadcastServiceTests
{
    private readonly Mock<IHubContext<WakeWordHub>> _hubContextMock;
    private readonly Mock<IHubClients> _clientsMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly Mock<ILogger<EventBroadcastService>> _loggerMock;

    public EventBroadcastServiceTests()
    {
        _hubContextMock = new Mock<IHubContext<WakeWordHub>>();
        _clientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();
        _loggerMock = new Mock<ILogger<EventBroadcastService>>();

        _hubContextMock.Setup(x => x.Clients).Returns(_clientsMock.Object);
        _clientsMock.Setup(x => x.All).Returns(_clientProxyMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Act
        var service = new EventBroadcastService(_hubContextMock.Object, _loggerMock.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public async Task BroadcastWakeWordDetectedAsync_ShouldSendToAllClients()
    {
        // Arrange
        var service = new EventBroadcastService(_hubContextMock.Object, _loggerMock.Object);
        var wakeWordEvent = new WakeWordEvent
        {
            Word = "jarvis",
            DetectedAt = DateTime.UtcNow,
            Confidence = 0.95f,
            ServiceVersion = "1.0.0"
        };

        // Act
        await service.BroadcastWakeWordDetectedAsync(wakeWordEvent);

        // Assert
        _clientProxyMock.Verify(
            x => x.SendCoreAsync(
                "WakeWordDetected",
                It.Is<object[]>(o => o.Length == 1 && o[0] == wakeWordEvent),
                default),
            Times.Once);
    }

    [Fact]
    public async Task BroadcastWakeWordDetectedAsync_ShouldLogInformation()
    {
        // Arrange
        var service = new EventBroadcastService(_hubContextMock.Object, _loggerMock.Object);
        var wakeWordEvent = new WakeWordEvent
        {
            Word = "jarvis",
            DetectedAt = DateTime.UtcNow,
            Confidence = 0.95f,
            ServiceVersion = "1.0.0"
        };

        // Act
        await service.BroadcastWakeWordDetectedAsync(wakeWordEvent);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Broadcasting wake word event")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task BroadcastWakeWordDetectedAsync_WithDifferentWords_ShouldBroadcastEach()
    {
        // Arrange
        var service = new EventBroadcastService(_hubContextMock.Object, _loggerMock.Object);
        var event1 = new WakeWordEvent { Word = "jarvis", DetectedAt = DateTime.UtcNow, Confidence = 0.95f };
        var event2 = new WakeWordEvent { Word = "alexa", DetectedAt = DateTime.UtcNow, Confidence = 0.88f };

        // Act
        await service.BroadcastWakeWordDetectedAsync(event1);
        await service.BroadcastWakeWordDetectedAsync(event2);

        // Assert
        _clientProxyMock.Verify(
            x => x.SendCoreAsync(
                "WakeWordDetected",
                It.IsAny<object[]>(),
                default),
            Times.Exactly(2));
    }
}
