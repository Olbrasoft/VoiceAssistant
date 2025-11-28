namespace Olbrasoft.VoiceAssistant.PushToTalkDictation.Tests;

public class KeyEventArgsTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldSetProperties()
    {
        // Arrange
        var key = KeyCode.CapsLock;
        var rawKeyCode = 58;
        var timestamp = DateTime.UtcNow;

        // Act
        var args = new KeyEventArgs(key, rawKeyCode, timestamp);

        // Assert
        Assert.Equal(key, args.Key);
        Assert.Equal(rawKeyCode, args.RawKeyCode);
        Assert.Equal(timestamp, args.Timestamp);
    }

    [Fact]
    public void Key_ShouldReturnCorrectKeyCode()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act & Assert
        Assert.Equal(KeyCode.Escape, new KeyEventArgs(KeyCode.Escape, 1, timestamp).Key);
        Assert.Equal(KeyCode.ScrollLock, new KeyEventArgs(KeyCode.ScrollLock, 70, timestamp).Key);
        Assert.Equal(KeyCode.NumLock, new KeyEventArgs(KeyCode.NumLock, 69, timestamp).Key);
    }

    [Fact]
    public void RawKeyCode_ShouldMatchKeyCodeValue()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var capsLockCode = (int)KeyCode.CapsLock;

        // Act
        var args = new KeyEventArgs(KeyCode.CapsLock, capsLockCode, timestamp);

        // Assert
        Assert.Equal(capsLockCode, args.RawKeyCode);
        Assert.Equal(58, args.RawKeyCode);
    }

    [Fact]
    public void KeyEventArgs_ShouldInheritFromEventArgs()
    {
        // Arrange & Act
        var args = new KeyEventArgs(KeyCode.Space, 57, DateTime.UtcNow);

        // Assert
        Assert.IsAssignableFrom<EventArgs>(args);
    }

    [Fact]
    public void Constructor_WithUnknownKeyCode_ShouldSetUnknown()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var args = new KeyEventArgs(KeyCode.Unknown, 999, timestamp);

        // Assert
        Assert.Equal(KeyCode.Unknown, args.Key);
        Assert.Equal(999, args.RawKeyCode);
    }

    [Fact]
    public void Timestamp_ShouldBeReadOnly()
    {
        // Arrange
        var originalTimestamp = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var args = new KeyEventArgs(KeyCode.Enter, 28, originalTimestamp);

        // Assert - timestamp should be immutable (no setter)
        Assert.Equal(originalTimestamp, args.Timestamp);
    }
}
