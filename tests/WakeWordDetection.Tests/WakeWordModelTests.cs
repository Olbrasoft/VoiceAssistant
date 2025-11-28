using Xunit;

namespace Olbrasoft.VoiceAssistant.WakeWordDetection.Tests;

public class WakeWordModelTests
{
    [Fact]
    public void Name_DefaultValue_IsEmptyString()
    {
        // Act
        var model = new WakeWordModel();

        // Assert
        Assert.Equal(string.Empty, model.Name);
    }

    [Fact]
    public void Name_WhenSet_ReturnsCorrectValue()
    {
        // Arrange & Act
        var model = new WakeWordModel { Name = "alexa_v0.1_t0.7" };

        // Assert
        Assert.Equal("alexa_v0.1_t0.7", model.Name);
    }

    [Fact]
    public void FilePath_DefaultValue_IsEmptyString()
    {
        // Act
        var model = new WakeWordModel();

        // Assert
        Assert.Equal(string.Empty, model.FilePath);
    }

    [Fact]
    public void FilePath_WhenSet_ReturnsCorrectValue()
    {
        // Arrange
        var filePath = "/home/user/models/alexa_v0.1_t0.7.onnx";

        // Act
        var model = new WakeWordModel { FilePath = filePath };

        // Assert
        Assert.Equal(filePath, model.FilePath);
    }

    [Fact]
    public void Version_DefaultValue_IsNull()
    {
        // Act
        var model = new WakeWordModel();

        // Assert
        Assert.Null(model.Version);
    }

    [Fact]
    public void Version_WhenSet_ReturnsCorrectValue()
    {
        // Arrange & Act
        var model = new WakeWordModel { Version = "0.1" };

        // Assert
        Assert.Equal("0.1", model.Version);
    }

    [Fact]
    public void Threshold_DefaultValue_IsZero()
    {
        // Act
        var model = new WakeWordModel();

        // Assert
        Assert.Equal(0f, model.Threshold);
    }

    [Fact]
    public void Threshold_WhenSet_ReturnsCorrectValue()
    {
        // Arrange & Act
        var model = new WakeWordModel { Threshold = 0.7f };

        // Assert
        Assert.Equal(0.7f, model.Threshold);
    }

    [Fact]
    public void HasExplicitThreshold_DefaultValue_IsFalse()
    {
        // Act
        var model = new WakeWordModel();

        // Assert
        Assert.False(model.HasExplicitThreshold);
    }

    [Fact]
    public void HasExplicitThreshold_WhenSetTrue_ReturnsTrue()
    {
        // Arrange & Act
        var model = new WakeWordModel { HasExplicitThreshold = true };

        // Assert
        Assert.True(model.HasExplicitThreshold);
    }

    [Fact]
    public void AllProperties_CanBeSetTogether()
    {
        // Arrange & Act
        var model = new WakeWordModel
        {
            Name = "hey_jarvis_v0.1_t0.5",
            FilePath = "/models/hey_jarvis_v0.1_t0.5.onnx",
            Version = "0.1",
            Threshold = 0.5f,
            HasExplicitThreshold = true
        };

        // Assert
        Assert.Equal("hey_jarvis_v0.1_t0.5", model.Name);
        Assert.Equal("/models/hey_jarvis_v0.1_t0.5.onnx", model.FilePath);
        Assert.Equal("0.1", model.Version);
        Assert.Equal(0.5f, model.Threshold);
        Assert.True(model.HasExplicitThreshold);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(0.7f)]
    [InlineData(1.0f)]
    public void Threshold_VariousValues_AcceptsValues(float threshold)
    {
        // Act
        var model = new WakeWordModel { Threshold = threshold };

        // Assert
        Assert.Equal(threshold, model.Threshold);
    }

    [Theory]
    [InlineData("0.1")]
    [InlineData("1.0")]
    [InlineData("2.5")]
    public void Version_VariousValues_AcceptsValues(string version)
    {
        // Act
        var model = new WakeWordModel { Version = version };

        // Assert
        Assert.Equal(version, model.Version);
    }
}
