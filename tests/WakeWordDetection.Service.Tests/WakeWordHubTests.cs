using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Olbrasoft.VoiceAssistant.WakeWordDetection.Service.Hubs;
using Xunit;

namespace Olbrasoft.VoiceAssistant.WakeWordDetection.Service.Tests;

/// <summary>
/// Unit tests for WakeWordHub class.
/// </summary>
public class WakeWordHubTests
{
    private readonly Mock<ILogger<WakeWordHub>> _loggerMock;
    private readonly Mock<HubCallerContext> _contextMock;
    private readonly Mock<IHubCallerClients> _clientsMock;
    private readonly Mock<ISingleClientProxy> _callerMock;

    public WakeWordHubTests()
    {
        _loggerMock = new Mock<ILogger<WakeWordHub>>();
        _contextMock = new Mock<HubCallerContext>();
        _clientsMock = new Mock<IHubCallerClients>();
        _callerMock = new Mock<ISingleClientProxy>();

        _contextMock.Setup(x => x.ConnectionId).Returns("test-connection-id");
        _clientsMock.Setup(x => x.Caller).Returns(_callerMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Act
        var hub = new WakeWordHub(_loggerMock.Object);

        // Assert
        Assert.NotNull(hub);
    }

    [Fact]
    public async Task OnConnectedAsync_ShouldLogConnectionAndSendMessage()
    {
        // Arrange
        var hub = new WakeWordHub(_loggerMock.Object)
        {
            Context = _contextMock.Object,
            Clients = _clientsMock.Object
        };

        // Act
        await hub.OnConnectedAsync();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Client connected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _callerMock.Verify(
            x => x.SendCoreAsync(
                "Connected",
                It.Is<object[]>(o => o.Length == 1 && o[0].ToString() == "test-connection-id"),
                default),
            Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_ShouldLogDisconnection()
    {
        // Arrange
        var hub = new WakeWordHub(_loggerMock.Object)
        {
            Context = _contextMock.Object,
            Clients = _clientsMock.Object
        };

        // Act
        await hub.OnDisconnectedAsync(null);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Client disconnected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_WithException_ShouldStillLogDisconnection()
    {
        // Arrange
        var hub = new WakeWordHub(_loggerMock.Object)
        {
            Context = _contextMock.Object,
            Clients = _clientsMock.Object
        };
        var exception = new Exception("Connection lost");

        // Act
        await hub.OnDisconnectedAsync(exception);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Client disconnected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Subscribe_ShouldLogAndSendSubscriptionConfirmation()
    {
        // Arrange
        var hub = new WakeWordHub(_loggerMock.Object)
        {
            Context = _contextMock.Object,
            Clients = _clientsMock.Object
        };
        var clientName = "TestClient";

        // Act
        await hub.Subscribe(clientName);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TestClient subscribed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _callerMock.Verify(
            x => x.SendCoreAsync(
                "Subscribed",
                It.Is<object[]>(o => o.Length == 1 && o[0].ToString() == clientName),
                default),
            Times.Once);
    }

    [Fact]
    public async Task Subscribe_WithDifferentNames_ShouldHandleEach()
    {
        // Arrange
        var hub = new WakeWordHub(_loggerMock.Object)
        {
            Context = _contextMock.Object,
            Clients = _clientsMock.Object
        };

        // Act
        await hub.Subscribe("Client1");
        await hub.Subscribe("Client2");

        // Assert
        _callerMock.Verify(
            x => x.SendCoreAsync(
                "Subscribed",
                It.IsAny<object[]>(),
                default),
            Times.Exactly(2));
    }
}
