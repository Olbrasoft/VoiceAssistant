namespace Olbrasoft.VoiceAssistant.PushToTalkDictation.Tests;

public class AudioDataEventArgsTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldSetProperties()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var timestamp = DateTime.UtcNow;

        // Act
        var args = new AudioDataEventArgs(data, timestamp);

        // Assert
        Assert.Equal(data, args.Data);
        Assert.Equal(timestamp, args.Timestamp);
    }

    [Fact]
    public void Constructor_WithEmptyData_ShouldSetEmptyArray()
    {
        // Arrange
        var data = Array.Empty<byte>();
        var timestamp = DateTime.UtcNow;

        // Act
        var args = new AudioDataEventArgs(data, timestamp);

        // Assert
        Assert.Empty(args.Data);
        Assert.Equal(timestamp, args.Timestamp);
    }

    [Fact]
    public void Data_ShouldReturnSameReferenceAsProvided()
    {
        // Arrange
        var data = new byte[] { 10, 20, 30 };
        var timestamp = DateTime.UtcNow;

        // Act
        var args = new AudioDataEventArgs(data, timestamp);

        // Assert
        Assert.Same(data, args.Data);
    }

    [Fact]
    public void AudioDataEventArgs_ShouldInheritFromEventArgs()
    {
        // Arrange & Act
        var args = new AudioDataEventArgs(Array.Empty<byte>(), DateTime.UtcNow);

        // Assert
        Assert.IsAssignableFrom<EventArgs>(args);
    }

    [Fact]
    public void Timestamp_ShouldPreserveKind()
    {
        // Arrange
        var localTime = DateTime.Now;
        var utcTime = DateTime.UtcNow;

        // Act
        var localArgs = new AudioDataEventArgs(Array.Empty<byte>(), localTime);
        var utcArgs = new AudioDataEventArgs(Array.Empty<byte>(), utcTime);

        // Assert
        Assert.Equal(DateTimeKind.Local, localArgs.Timestamp.Kind);
        Assert.Equal(DateTimeKind.Utc, utcArgs.Timestamp.Kind);
    }
}
