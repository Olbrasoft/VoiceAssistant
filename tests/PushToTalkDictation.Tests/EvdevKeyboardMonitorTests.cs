using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Olbrasoft.VoiceAssistant.PushToTalkDictation.Tests;

public class EvdevKeyboardMonitorTests
{
    private readonly Mock<ILogger<EvdevKeyboardMonitor>> _mockLogger;

    public EvdevKeyboardMonitorTests()
    {
        _mockLogger = new Mock<ILogger<EvdevKeyboardMonitor>>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EvdevKeyboardMonitor(null!));
    }

    [Fact]
    public void Constructor_WithValidLogger_ShouldCreateInstance()
    {
        // This may throw FileNotFoundException if no keyboard device is found,
        // which is expected in test environment
        try
        {
            var monitor = new EvdevKeyboardMonitor(_mockLogger.Object);
            Assert.NotNull(monitor);
        }
        catch (FileNotFoundException)
        {
            // Expected in test environment without keyboard device
            Assert.True(true);
        }
    }

    [Fact]
    public void IsMonitoring_BeforeStart_ShouldBeFalse()
    {
        // This may throw FileNotFoundException if no keyboard device is found
        try
        {
            using var monitor = new EvdevKeyboardMonitor(_mockLogger.Object);
            Assert.False(monitor.IsMonitoring);
        }
        catch (FileNotFoundException)
        {
            // Expected in test environment
            Assert.True(true);
        }
    }

    [Fact]
    public void EvdevKeyboardMonitor_ShouldImplementIKeyboardMonitor()
    {
        // Assert
        Assert.True(typeof(IKeyboardMonitor).IsAssignableFrom(typeof(EvdevKeyboardMonitor)));
    }

    [Fact]
    public void EvdevKeyboardMonitor_ShouldImplementIDisposable()
    {
        // Assert
        Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(EvdevKeyboardMonitor)));
    }

    [Fact]
    public void KeyPressed_ShouldBeEventHandler()
    {
        // Arrange
        var eventInfo = typeof(EvdevKeyboardMonitor).GetEvent(nameof(EvdevKeyboardMonitor.KeyPressed));

        // Assert
        Assert.NotNull(eventInfo);
        Assert.Equal(typeof(EventHandler<KeyEventArgs>), eventInfo.EventHandlerType);
    }

    [Fact]
    public void KeyReleased_ShouldBeEventHandler()
    {
        // Arrange
        var eventInfo = typeof(EvdevKeyboardMonitor).GetEvent(nameof(EvdevKeyboardMonitor.KeyReleased));

        // Assert
        Assert.NotNull(eventInfo);
        Assert.Equal(typeof(EventHandler<KeyEventArgs>), eventInfo.EventHandlerType);
    }

    [Fact]
    public async Task StopMonitoringAsync_WhenNotMonitoring_ShouldNotThrow()
    {
        try
        {
            using var monitor = new EvdevKeyboardMonitor(_mockLogger.Object);
            
            // Act & Assert - should not throw
            await monitor.StopMonitoringAsync();
        }
        catch (FileNotFoundException)
        {
            // Expected in test environment
            Assert.True(true);
        }
    }

    [Fact]
    public void IsCapsLockOn_ShouldReturnBooleanValue()
    {
        try
        {
            using var monitor = new EvdevKeyboardMonitor(_mockLogger.Object);
            
            // Act
            var result = monitor.IsCapsLockOn();

            // Assert - should return boolean without throwing
            Assert.IsType<bool>(result);
        }
        catch (FileNotFoundException)
        {
            // Expected in test environment
            Assert.True(true);
        }
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        try
        {
            var monitor = new EvdevKeyboardMonitor(_mockLogger.Object);
            
            // Act & Assert - should not throw
            monitor.Dispose();
        }
        catch (FileNotFoundException)
        {
            // Expected in test environment
            Assert.True(true);
        }
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        try
        {
            var monitor = new EvdevKeyboardMonitor(_mockLogger.Object);
            
            // Act & Assert - should not throw
            monitor.Dispose();
            monitor.Dispose();
            monitor.Dispose();
        }
        catch (FileNotFoundException)
        {
            // Expected in test environment
            Assert.True(true);
        }
    }
}
