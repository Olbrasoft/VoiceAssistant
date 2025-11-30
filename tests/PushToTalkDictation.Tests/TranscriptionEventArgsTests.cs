using Xunit;

namespace Olbrasoft.VoiceAssistant.PushToTalkDictation.Tests;

public class TranscriptionEventArgsTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldSetProperties()
    {
        // Arrange
        var text = "Hello World";
        var confidence = 0.95f;
        var timestamp = DateTime.UtcNow;

        // Act
        var args = new TranscriptionEventArgs(text, confidence, timestamp);

        // Assert
        Assert.Equal(text, args.Text);
        Assert.Equal(confidence, args.Confidence);
        Assert.Equal(timestamp, args.Timestamp);
    }

    [Fact]
    public void Constructor_WithEmptyText_ShouldSetEmptyString()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var args = new TranscriptionEventArgs(string.Empty, 0f, timestamp);

        // Assert
        Assert.Equal(string.Empty, args.Text);
    }

    [Fact]
    public void Confidence_ShouldAcceptZero()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var args = new TranscriptionEventArgs("test", 0f, timestamp);

        // Assert
        Assert.Equal(0f, args.Confidence);
    }

    [Fact]
    public void Confidence_ShouldAcceptOne()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var args = new TranscriptionEventArgs("test", 1f, timestamp);

        // Assert
        Assert.Equal(1f, args.Confidence);
    }

    [Fact]
    public void Confidence_ShouldAcceptMidRangeValues()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var args = new TranscriptionEventArgs("test", 0.5f, timestamp);

        // Assert
        Assert.Equal(0.5f, args.Confidence);
    }

    [Fact]
    public void TranscriptionEventArgs_ShouldInheritFromEventArgs()
    {
        // Arrange & Act
        var args = new TranscriptionEventArgs("test", 0.9f, DateTime.UtcNow);

        // Assert
        Assert.IsAssignableFrom<EventArgs>(args);
    }

    [Fact]
    public void Text_ShouldPreserveSpecialCharacters()
    {
        // Arrange
        var textWithSpecialChars = "Dobrý den! Jak se máte? 🎤";
        var timestamp = DateTime.UtcNow;

        // Act
        var args = new TranscriptionEventArgs(textWithSpecialChars, 0.85f, timestamp);

        // Assert
        Assert.Equal(textWithSpecialChars, args.Text);
    }

    [Fact]
    public void Text_ShouldPreserveWhitespace()
    {
        // Arrange
        var textWithWhitespace = "  Hello   World  ";
        var timestamp = DateTime.UtcNow;

        // Act
        var args = new TranscriptionEventArgs(textWithWhitespace, 0.9f, timestamp);

        // Assert
        Assert.Equal(textWithWhitespace, args.Text);
    }
}
