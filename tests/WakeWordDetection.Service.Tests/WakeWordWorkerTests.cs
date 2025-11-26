using Microsoft.Extensions.Logging;
using Moq;
using Olbrasoft.VoiceAssistant.WakeWordDetection.Service;
using Olbrasoft.VoiceAssistant.WakeWordDetection.Service.Models;
using Olbrasoft.VoiceAssistant.WakeWordDetection.Service.Services;
using Xunit;

namespace Olbrasoft.VoiceAssistant.WakeWordDetection.Service.Tests;

/// <summary>
/// Unit tests for WakeWordWorker class.
/// </summary>
public class WakeWordWorkerTests
{
    private readonly Mock<IWakeWordDetector> _detectorMock;
    private readonly Mock<IEventBroadcastService> _eventBroadcastMock;
    private readonly Mock<ILogger<WakeWordWorker>> _loggerMock;

    public WakeWordWorkerTests()
    {
        _detectorMock = new Mock<IWakeWordDetector>();
        _eventBroadcastMock = new Mock<IEventBroadcastService>();
        _loggerMock = new Mock<ILogger<WakeWordWorker>>();
    }

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Act
        var worker = new WakeWordWorker(
            _detectorMock.Object,
            _eventBroadcastMock.Object,
            _loggerMock.Object);

        // Assert
        Assert.NotNull(worker);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStartListening()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _detectorMock
            .Setup(x => x.StartListeningAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.Delay(Timeout.Infinite, cts.Token));

        var worker = new WakeWordWorker(
            _detectorMock.Object,
            _eventBroadcastMock.Object,
            _loggerMock.Object);

        // Act
        var executeTask = worker.StartAsync(cts.Token);
        await Task.Delay(200); // Give it time to start

        // Assert
        _detectorMock.Verify(x => x.StartListeningAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Cleanup
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task WhenWakeWordDetected_ShouldBroadcastEvent()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _detectorMock
            .Setup(x => x.StartListeningAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.Delay(Timeout.Infinite, cts.Token));

        var worker = new WakeWordWorker(
            _detectorMock.Object,
            _eventBroadcastMock.Object,
            _loggerMock.Object);

        WakeWordEvent? broadcastedEvent = null;
        _eventBroadcastMock
            .Setup(x => x.BroadcastWakeWordDetectedAsync(It.IsAny<WakeWordEvent>()))
            .Callback<WakeWordEvent>(e => broadcastedEvent = e)
            .Returns(Task.CompletedTask);

        // Act
        await worker.StartAsync(cts.Token);
        await Task.Delay(100);

        // Trigger wake word detection
        var eventArgs = new WakeWordDetectedEventArgs 
        { 
            DetectedWord = "jarvis", 
            DetectedAt = DateTime.UtcNow, 
            Confidence = 0.95f 
        };
        _detectorMock.Raise(x => x.WakeWordDetected += null, null, eventArgs);

        await Task.Delay(100); // Give event handler time to execute

        // Assert
        Assert.NotNull(broadcastedEvent);
        Assert.Equal("jarvis", broadcastedEvent.Word);
        Assert.Equal(0.95f, broadcastedEvent.Confidence);

        // Cleanup
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_ShouldStopListening()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _detectorMock
            .Setup(x => x.StartListeningAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.Delay(Timeout.Infinite, cts.Token));
        _detectorMock
            .Setup(x => x.StopListeningAsync())
            .Returns(Task.CompletedTask);

        var worker = new WakeWordWorker(
            _detectorMock.Object,
            _eventBroadcastMock.Object,
            _loggerMock.Object);

        await worker.StartAsync(cts.Token);
        await Task.Delay(100);

        // Act
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        // Assert
        _detectorMock.Verify(x => x.StopListeningAsync(), Times.Once);
    }

    [Fact]
    public async Task WakeWordDetection_ShouldIncludeServiceVersion()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _detectorMock
            .Setup(x => x.StartListeningAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.Delay(Timeout.Infinite, cts.Token));

        var worker = new WakeWordWorker(
            _detectorMock.Object,
            _eventBroadcastMock.Object,
            _loggerMock.Object);

        WakeWordEvent? broadcastedEvent = null;
        _eventBroadcastMock
            .Setup(x => x.BroadcastWakeWordDetectedAsync(It.IsAny<WakeWordEvent>()))
            .Callback<WakeWordEvent>(e => broadcastedEvent = e)
            .Returns(Task.CompletedTask);

        // Act
        await worker.StartAsync(cts.Token);
        await Task.Delay(100);

        var eventArgs = new WakeWordDetectedEventArgs 
        { 
            DetectedWord = "jarvis", 
            DetectedAt = DateTime.UtcNow, 
            Confidence = 0.87f 
        };
        _detectorMock.Raise(x => x.WakeWordDetected += null, null, eventArgs);
        await Task.Delay(100);

        // Assert
        Assert.NotNull(broadcastedEvent);
        Assert.Equal("1.0.0", broadcastedEvent.ServiceVersion);

        // Cleanup
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);
    }
}
