using Xunit;

namespace Olbrasoft.VoiceAssistant.WakeWordDetection.Tests;

public class WakeWordDetectedEventArgsTests
{
    [Fact]
    public void DetectedWord_DefaultValue_IsEmptyString()
    {
        // Act
        var args = new WakeWordDetectedEventArgs();

        // Assert
        Assert.Equal(string.Empty, args.DetectedWord);
    }

    [Fact]
    public void DetectedWord_WhenSet_ReturnsCorrectValue()
    {
        // Arrange & Act
        var args = new WakeWordDetectedEventArgs { DetectedWord = "jarvis" };

        // Assert
        Assert.Equal("jarvis", args.DetectedWord);
    }

    [Fact]
    public void DetectedAt_WhenSet_ReturnsCorrectValue()
    {
        // Arrange
        var detectedAt = new DateTime(2024, 1, 15, 12, 30, 45, DateTimeKind.Utc);

        // Act
        var args = new WakeWordDetectedEventArgs { DetectedAt = detectedAt };

        // Assert
        Assert.Equal(detectedAt, args.DetectedAt);
    }

    [Fact]
    public void Confidence_WhenSet_ReturnsCorrectValue()
    {
        // Arrange & Act
        var args = new WakeWordDetectedEventArgs { Confidence = 0.95f };

        // Assert
        Assert.Equal(0.95f, args.Confidence);
    }

    [Fact]
    public void AllProperties_CanBeSetTogether()
    {
        // Arrange
        var detectedAt = DateTime.UtcNow;

        // Act
        var args = new WakeWordDetectedEventArgs
        {
            DetectedWord = "alexa",
            DetectedAt = detectedAt,
            Confidence = 0.87f
        };

        // Assert
        Assert.Equal("alexa", args.DetectedWord);
        Assert.Equal(detectedAt, args.DetectedAt);
        Assert.Equal(0.87f, args.Confidence);
    }

    [Fact]
    public void InheritsFromEventArgs()
    {
        // Assert
        Assert.True(typeof(WakeWordDetectedEventArgs).IsSubclassOf(typeof(EventArgs)));
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    public void Confidence_ValidRange_AcceptsValues(float confidence)
    {
        // Act
        var args = new WakeWordDetectedEventArgs { Confidence = confidence };

        // Assert
        Assert.Equal(confidence, args.Confidence);
    }

    [Theory]
    [InlineData("jarvis")]
    [InlineData("alexa")]
    [InlineData("hey mycroft")]
    [InlineData("hey rhasspy")]
    public void DetectedWord_VariousWakeWords_AcceptsValues(string wakeWord)
    {
        // Act
        var args = new WakeWordDetectedEventArgs { DetectedWord = wakeWord };

        // Assert
        Assert.Equal(wakeWord, args.DetectedWord);
    }
}
