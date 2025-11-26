using Olbrasoft.VoiceAssistant.WakeWordDetection.Service.Models;
using Xunit;

namespace Olbrasoft.VoiceAssistant.WakeWordDetection.Service.Tests;

/// <summary>
/// Unit tests for WakeWordEvent model.
/// </summary>
public class WakeWordEventTests
{
    [Fact]
    public void Properties_ShouldInitializeCorrectly()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var word = "Jarvis";
        var confidence = 0.95f;
        var version = "1.0.0";

        // Act
        var wakeWordEvent = new WakeWordEvent
        {
            DetectedAt = timestamp,
            Word = word,
            Confidence = confidence,
            ServiceVersion = version
        };

        // Assert
        Assert.Equal(timestamp, wakeWordEvent.DetectedAt);
        Assert.Equal(word, wakeWordEvent.Word);
        Assert.Equal(confidence, wakeWordEvent.Confidence);
        Assert.Equal(version, wakeWordEvent.ServiceVersion);
    }

    [Fact]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        // Act
        var wakeWordEvent = new WakeWordEvent();

        // Assert
        Assert.Equal(string.Empty, wakeWordEvent.Word);
        Assert.Equal(default(DateTime), wakeWordEvent.DetectedAt);
        Assert.Equal(0.0f, wakeWordEvent.Confidence);
        Assert.Equal("1.0.0", wakeWordEvent.ServiceVersion);
    }

    [Fact]
    public void Properties_ShouldHandleMinimumValues()
    {
        // Arrange
        var timestamp = DateTime.MinValue;
        var word = "";
        var confidence = 0.0f;
        var version = "";

        // Act
        var wakeWordEvent = new WakeWordEvent
        {
            DetectedAt = timestamp,
            Word = word,
            Confidence = confidence,
            ServiceVersion = version
        };

        // Assert
        Assert.Equal(timestamp, wakeWordEvent.DetectedAt);
        Assert.Equal(word, wakeWordEvent.Word);
        Assert.Equal(confidence, wakeWordEvent.Confidence);
        Assert.Equal(version, wakeWordEvent.ServiceVersion);
    }

    [Fact]
    public void Properties_ShouldHandleMaximumValues()
    {
        // Arrange
        var timestamp = DateTime.MaxValue;
        var word = new string('A', 1000); // Very long string
        var confidence = 1.0f;
        var version = new string('1', 100);

        // Act
        var wakeWordEvent = new WakeWordEvent
        {
            DetectedAt = timestamp,
            Word = word,
            Confidence = confidence,
            ServiceVersion = version
        };

        // Assert
        Assert.Equal(timestamp, wakeWordEvent.DetectedAt);
        Assert.Equal(word, wakeWordEvent.Word);
        Assert.Equal(confidence, wakeWordEvent.Confidence);
        Assert.Equal(version, wakeWordEvent.ServiceVersion);
    }

    [Fact]
    public void RecordEquality_ShouldReturnTrueForIdenticalValues()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var word = "Jarvis";
        var confidence = 0.95f;
        var version = "1.0.0";

        var event1 = new WakeWordEvent
        {
            DetectedAt = timestamp,
            Word = word,
            Confidence = confidence,
            ServiceVersion = version
        };

        var event2 = new WakeWordEvent
        {
            DetectedAt = timestamp,
            Word = word,
            Confidence = confidence,
            ServiceVersion = version
        };

        // Act & Assert
        Assert.Equal(event1, event2);
        Assert.True(event1 == event2);
        Assert.False(event1 != event2);
    }

    [Fact]
    public void RecordEquality_ShouldReturnFalseForDifferentDetectedAt()
    {
        // Arrange
        var timestamp1 = DateTime.UtcNow;
        var timestamp2 = DateTime.UtcNow.AddSeconds(1);
        var word = "Jarvis";
        var confidence = 0.95f;

        var event1 = new WakeWordEvent { DetectedAt = timestamp1, Word = word, Confidence = confidence };
        var event2 = new WakeWordEvent { DetectedAt = timestamp2, Word = word, Confidence = confidence };

        // Act & Assert
        Assert.NotEqual(event1, event2);
        Assert.False(event1 == event2);
        Assert.True(event1 != event2);
    }

    [Fact]
    public void RecordEquality_ShouldReturnFalseForDifferentWord()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var word1 = "Jarvis";
        var word2 = "Alexa";
        var confidence = 0.95f;

        var event1 = new WakeWordEvent { DetectedAt = timestamp, Word = word1, Confidence = confidence };
        var event2 = new WakeWordEvent { DetectedAt = timestamp, Word = word2, Confidence = confidence };

        // Act & Assert
        Assert.NotEqual(event1, event2);
        Assert.False(event1 == event2);
        Assert.True(event1 != event2);
    }

    [Fact]
    public void RecordEquality_ShouldReturnFalseForDifferentConfidence()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var word = "Jarvis";
        var confidence1 = 0.95f;
        var confidence2 = 0.85f;

        var event1 = new WakeWordEvent { DetectedAt = timestamp, Word = word, Confidence = confidence1 };
        var event2 = new WakeWordEvent { DetectedAt = timestamp, Word = word, Confidence = confidence2 };

        // Act & Assert
        Assert.NotEqual(event1, event2);
        Assert.False(event1 == event2);
        Assert.True(event1 != event2);
    }

    [Fact]
    public void RecordEquality_ShouldReturnFalseForDifferentServiceVersion()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var word = "Jarvis";
        var confidence = 0.95f;
        var version1 = "1.0.0";
        var version2 = "2.0.0";

        var event1 = new WakeWordEvent { DetectedAt = timestamp, Word = word, Confidence = confidence, ServiceVersion = version1 };
        var event2 = new WakeWordEvent { DetectedAt = timestamp, Word = word, Confidence = confidence, ServiceVersion = version2 };

        // Act & Assert
        Assert.NotEqual(event1, event2);
        Assert.False(event1 == event2);
        Assert.True(event1 != event2);
    }

    [Fact]
    public void GetHashCode_ShouldBeSameForEqualRecords()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var word = "Jarvis";
        var confidence = 0.95f;
        var version = "1.0.0";

        var event1 = new WakeWordEvent { DetectedAt = timestamp, Word = word, Confidence = confidence, ServiceVersion = version };
        var event2 = new WakeWordEvent { DetectedAt = timestamp, Word = word, Confidence = confidence, ServiceVersion = version };

        // Act & Assert
        Assert.Equal(event1.GetHashCode(), event2.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldContainAllProperties()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 15, 10, 30, 45, DateTimeKind.Utc);
        var word = "Jarvis";
        var confidence = 0.95f;
        var version = "1.0.0";

        var wakeWordEvent = new WakeWordEvent
        {
            DetectedAt = timestamp,
            Word = word,
            Confidence = confidence,
            ServiceVersion = version
        };

        // Act
        var result = wakeWordEvent.ToString();

        // Assert
        Assert.Contains("WakeWordEvent", result);
        Assert.Contains("DetectedAt", result);
        Assert.Contains("Word", result);
        Assert.Contains("Confidence", result);
        Assert.Contains("ServiceVersion", result);
    }
}
