using Olbrasoft.VoiceAssistant.Orchestration.Models;

namespace Olbrasoft.VoiceAssistant.Orchestration.Tests.Models;

public class WakeWordEventTests
{
    [Fact]
    public void Word_DefaultValue_ShouldBeEmptyString()
    {
        // Arrange & Act
        var wakeWordEvent = new WakeWordEvent();

        // Assert
        Assert.Equal(string.Empty, wakeWordEvent.Word);
    }

    [Fact]
    public void DetectedAt_DefaultValue_ShouldBeDefaultDateTime()
    {
        // Arrange & Act
        var wakeWordEvent = new WakeWordEvent();

        // Assert
        Assert.Equal(default, wakeWordEvent.DetectedAt);
    }

    [Fact]
    public void Confidence_DefaultValue_ShouldBeZero()
    {
        // Arrange & Act
        var wakeWordEvent = new WakeWordEvent();

        // Assert
        Assert.Equal(0f, wakeWordEvent.Confidence);
    }

    [Fact]
    public void ServiceVersion_DefaultValue_ShouldBe100()
    {
        // Arrange & Act
        var wakeWordEvent = new WakeWordEvent();

        // Assert
        Assert.Equal("1.0.0", wakeWordEvent.ServiceVersion);
    }

    [Fact]
    public void Properties_WhenSet_ShouldReturnSetValues()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var wakeWordEvent = new WakeWordEvent
        {
            Word = "hey_jarvis_v0.1_t0.5",
            DetectedAt = now,
            Confidence = 0.95f,
            ServiceVersion = "2.0.0"
        };

        // Assert
        Assert.Equal("hey_jarvis_v0.1_t0.5", wakeWordEvent.Word);
        Assert.Equal(now, wakeWordEvent.DetectedAt);
        Assert.Equal(0.95f, wakeWordEvent.Confidence);
        Assert.Equal("2.0.0", wakeWordEvent.ServiceVersion);
    }

    [Fact]
    public void Record_ShouldSupportEquality()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var event1 = new WakeWordEvent
        {
            Word = "jarvis",
            DetectedAt = now,
            Confidence = 0.9f,
            ServiceVersion = "1.0.0"
        };

        var event2 = new WakeWordEvent
        {
            Word = "jarvis",
            DetectedAt = now,
            Confidence = 0.9f,
            ServiceVersion = "1.0.0"
        };

        // Assert
        Assert.Equal(event1, event2);
    }

    [Fact]
    public void Record_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var event1 = new WakeWordEvent { Word = "jarvis" };
        var event2 = new WakeWordEvent { Word = "alexa" };

        // Assert
        Assert.NotEqual(event1, event2);
    }

    [Fact]
    public void Record_ShouldSupportWith()
    {
        // Arrange
        var original = new WakeWordEvent
        {
            Word = "jarvis",
            Confidence = 0.8f
        };

        // Act
        var modified = original with { Confidence = 0.95f };

        // Assert
        Assert.Equal("jarvis", modified.Word);
        Assert.Equal(0.95f, modified.Confidence);
        Assert.Equal(0.8f, original.Confidence); // Original unchanged
    }
}
