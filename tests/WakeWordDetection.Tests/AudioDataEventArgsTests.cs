using Xunit;

namespace Olbrasoft.VoiceAssistant.WakeWordDetection.Tests;

public class AudioDataEventArgsTests
{
    [Fact]
    public void AudioData_WhenSet_ReturnsCorrectValue()
    {
        // Arrange
        var audioData = new short[] { 100, 200, 300, 400, 500 };

        // Act
        var args = new AudioDataEventArgs { AudioData = audioData };

        // Assert
        Assert.Equal(audioData, args.AudioData);
    }

    [Fact]
    public void SampleCount_ReturnsLengthOfAudioData()
    {
        // Arrange
        var audioData = new short[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        // Act
        var args = new AudioDataEventArgs { AudioData = audioData };

        // Assert
        Assert.Equal(10, args.SampleCount);
    }

    [Fact]
    public void SampleCount_EmptyArray_ReturnsZero()
    {
        // Arrange
        var audioData = Array.Empty<short>();

        // Act
        var args = new AudioDataEventArgs { AudioData = audioData };

        // Assert
        Assert.Equal(0, args.SampleCount);
    }

    [Fact]
    public void Timestamp_DefaultValue_IsUtcNow()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var args = new AudioDataEventArgs { AudioData = new short[] { 1 } };
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.True(args.Timestamp >= beforeCreation);
        Assert.True(args.Timestamp <= afterCreation);
    }

    [Fact]
    public void Timestamp_WhenExplicitlySet_ReturnsSetValue()
    {
        // Arrange
        var customTimestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var args = new AudioDataEventArgs 
        { 
            AudioData = new short[] { 1, 2, 3 },
            Timestamp = customTimestamp 
        };

        // Assert
        Assert.Equal(customTimestamp, args.Timestamp);
    }

    [Fact]
    public void InheritsFromEventArgs()
    {
        // Assert
        Assert.True(typeof(AudioDataEventArgs).IsSubclassOf(typeof(EventArgs)));
    }

    [Fact]
    public void AudioData_LargeArray_HandledCorrectly()
    {
        // Arrange - typical audio frame size
        var audioData = new short[512];
        for (int i = 0; i < audioData.Length; i++)
        {
            audioData[i] = (short)(i % short.MaxValue);
        }

        // Act
        var args = new AudioDataEventArgs { AudioData = audioData };

        // Assert
        Assert.Equal(512, args.SampleCount);
        Assert.Equal(audioData, args.AudioData);
    }
}
