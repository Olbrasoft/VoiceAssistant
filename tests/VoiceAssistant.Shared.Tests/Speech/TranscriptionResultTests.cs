using FluentAssertions;
using Olbrasoft.VoiceAssistant.Shared.Speech;
using Xunit;

namespace VoiceAssistant.Shared.Tests.Speech;

public class TranscriptionResultTests
{
    [Fact]
    public void Constructor_WithSuccessfulTranscription_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var result = new TranscriptionResult("hello world", 0.95f);

        // Assert
        result.Text.Should().Be("hello world");
        result.Confidence.Should().Be(0.95f);
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithError_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var result = new TranscriptionResult("Model not loaded");

        // Assert
        result.Text.Should().BeEmpty();
        result.Confidence.Should().Be(0.0f);
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Model not loaded");
    }
}
