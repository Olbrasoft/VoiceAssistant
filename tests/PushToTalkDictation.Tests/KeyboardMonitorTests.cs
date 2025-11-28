using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Olbrasoft.VoiceAssistant.PushToTalkDictation.Tests;

public class KeyboardMonitorTests
{
    private readonly Mock<ILogger<EvdevKeyboardMonitor>> _mockLogger;

    public KeyboardMonitorTests()
    {
        _mockLogger = new Mock<ILogger<EvdevKeyboardMonitor>>();
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EvdevKeyboardMonitor(null!));
    }

    [Fact]
    public void Constructor_ValidLogger_CreatesInstance()
    {
        // Arrange & Act
        var monitor = new EvdevKeyboardMonitor(_mockLogger.Object, "/dev/input/event0");

        // Assert
        Assert.NotNull(monitor);
        Assert.False(monitor.IsMonitoring);
    }

    [Fact]
    public void IsMonitoring_InitialState_ReturnsFalse()
    {
        // Arrange
        var monitor = new EvdevKeyboardMonitor(_mockLogger.Object, "/dev/input/event0");

        // Act & Assert
        Assert.False(monitor.IsMonitoring);
    }

    [Fact]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        // Arrange
        var monitor = new EvdevKeyboardMonitor(_mockLogger.Object, "/dev/input/event0");

        // Act & Assert
        monitor.Dispose();
        monitor.Dispose(); // Should not throw
    }

    [Fact]
    public void KeyEventArgs_Constructor_SetsProperties()
    {
        // Arrange
        var key = KeyCode.CapsLock;
        var rawCode = 58;
        var timestamp = DateTime.UtcNow;

        // Act
        var eventArgs = new KeyEventArgs(key, rawCode, timestamp);

        // Assert
        Assert.Equal(key, eventArgs.Key);
        Assert.Equal(rawCode, eventArgs.RawKeyCode);
        Assert.Equal(timestamp, eventArgs.Timestamp);
    }

    [Theory]
    [InlineData(KeyCode.CapsLock, 58)]
    [InlineData(KeyCode.ScrollLock, 70)]
    [InlineData(KeyCode.NumLock, 69)]
    [InlineData(KeyCode.LeftControl, 29)]
    [InlineData(KeyCode.Space, 57)]
    public void KeyCode_EnumValues_MatchLinuxKeyCodes(KeyCode keyCode, int expectedValue)
    {
        // Arrange & Act
        var actualValue = (int)keyCode;

        // Assert
        Assert.Equal(expectedValue, actualValue);
    }

    // Note: Tests requiring actual /dev/input/eventX access are skipped
    // These tests would require:
    // 1. Running as root or user in 'input' group
    // 2. Actual keyboard hardware or virtual input device
    // For integration testing, use a test harness with mock input events
}
