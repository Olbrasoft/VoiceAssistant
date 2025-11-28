using Xunit;

namespace Olbrasoft.VoiceAssistant.WakeWordDetection.Tests;

public class FileBasedModelProviderTests
{
    [Fact]
    public void Constructor_DefaultThreshold_IsHalf()
    {
        // Arrange
        var provider = new FileBasedModelProvider();

        // Act
        var model = provider.GetModel("/path/to/model_without_threshold.onnx");

        // Assert
        Assert.Equal(0.5f, model.Threshold);
        Assert.False(model.HasExplicitThreshold);
    }

    [Fact]
    public void Constructor_CustomDefaultThreshold_IsUsed()
    {
        // Arrange
        var provider = new FileBasedModelProvider(defaultThreshold: 0.7f);

        // Act
        var model = provider.GetModel("/path/to/model_without_threshold.onnx");

        // Assert
        Assert.Equal(0.7f, model.Threshold);
        Assert.False(model.HasExplicitThreshold);
    }

    [Fact]
    public void GetModel_NullIdentifier_ThrowsArgumentException()
    {
        // Arrange
        var provider = new FileBasedModelProvider();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => provider.GetModel(null!));
    }

    [Fact]
    public void GetModel_EmptyIdentifier_ThrowsArgumentException()
    {
        // Arrange
        var provider = new FileBasedModelProvider();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => provider.GetModel(string.Empty));
    }

    [Fact]
    public void GetModel_WhitespaceIdentifier_ThrowsArgumentException()
    {
        // Arrange
        var provider = new FileBasedModelProvider();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => provider.GetModel("   "));
    }

    [Theory]
    [InlineData("/models/alexa_v0.1_t0.7.onnx", 0.7f, true)]
    [InlineData("/models/hey_jarvis_v0.1_t0.5.onnx", 0.5f, true)]
    [InlineData("/models/model_t0.6.onnx", 0.6f, true)]
    [InlineData("/models/model_t.9.onnx", 0.9f, true)]
    public void GetModel_WithThresholdInFilename_ParsesThresholdCorrectly(
        string filePath, float expectedThreshold, bool hasExplicitThreshold)
    {
        // Arrange
        var provider = new FileBasedModelProvider();

        // Act
        var model = provider.GetModel(filePath);

        // Assert
        Assert.Equal(expectedThreshold, model.Threshold);
        Assert.Equal(hasExplicitThreshold, model.HasExplicitThreshold);
    }

    [Theory]
    [InlineData("/models/alexa_v0.1_t0.7.onnx", "0.1")]
    [InlineData("/models/hey_jarvis_v0.1_t0.5.onnx", "0.1")]
    [InlineData("/models/model_v1.0.onnx", "1.0")]
    [InlineData("/models/model_v2.5.onnx", "2.5")]
    public void GetModel_WithVersionInFilename_ParsesVersionCorrectly(
        string filePath, string expectedVersion)
    {
        // Arrange
        var provider = new FileBasedModelProvider();

        // Act
        var model = provider.GetModel(filePath);

        // Assert
        Assert.Equal(expectedVersion, model.Version);
    }

    [Fact]
    public void GetModel_WithoutVersionInFilename_VersionIsNull()
    {
        // Arrange
        var provider = new FileBasedModelProvider();

        // Act
        var model = provider.GetModel("/models/simple_model.onnx");

        // Assert
        Assert.Null(model.Version);
    }

    [Fact]
    public void GetModel_SetsNameCorrectly()
    {
        // Arrange
        var provider = new FileBasedModelProvider();

        // Act
        var model = provider.GetModel("/models/alexa_v0.1_t0.7.onnx");

        // Assert
        Assert.Equal("alexa_v0.1_t0.7", model.Name);
    }

    [Fact]
    public void GetModel_SetsFilePathCorrectly()
    {
        // Arrange
        var provider = new FileBasedModelProvider();
        var filePath = "/models/alexa_v0.1_t0.7.onnx";

        // Act
        var model = provider.GetModel(filePath);

        // Assert
        Assert.Equal(filePath, model.FilePath);
    }

    [Fact]
    public void GetModels_ReturnsModelsForAllIdentifiers()
    {
        // Arrange
        var provider = new FileBasedModelProvider();
        var identifiers = new[]
        {
            "/models/alexa_v0.1_t0.7.onnx",
            "/models/hey_jarvis_v0.1_t0.5.onnx",
            "/models/simple_model.onnx"
        };

        // Act
        var models = provider.GetModels(identifiers).ToList();

        // Assert
        Assert.Equal(3, models.Count);
        Assert.Equal("alexa_v0.1_t0.7", models[0].Name);
        Assert.Equal("hey_jarvis_v0.1_t0.5", models[1].Name);
        Assert.Equal("simple_model", models[2].Name);
    }

    [Fact]
    public void GetModels_EmptyCollection_ReturnsEmpty()
    {
        // Arrange
        var provider = new FileBasedModelProvider();

        // Act
        var models = provider.GetModels(Array.Empty<string>()).ToList();

        // Assert
        Assert.Empty(models);
    }

    [Fact]
    public void GetModel_CaseInsensitiveThresholdParsing()
    {
        // Arrange
        var provider = new FileBasedModelProvider();

        // Act - uppercase T
        var model = provider.GetModel("/models/model_T0.8.onnx");

        // Assert
        Assert.Equal(0.8f, model.Threshold);
        Assert.True(model.HasExplicitThreshold);
    }

    [Fact]
    public void GetModel_CaseInsensitiveVersionParsing()
    {
        // Arrange
        var provider = new FileBasedModelProvider();

        // Act - uppercase V
        var model = provider.GetModel("/models/model_V1.5.onnx");

        // Assert
        Assert.Equal("1.5", model.Version);
    }

    [Fact]
    public void GetModel_TfliteExtension_WorksCorrectly()
    {
        // Arrange
        var provider = new FileBasedModelProvider();

        // Act
        var model = provider.GetModel("/models/hey_jarvis.tflite");

        // Assert
        Assert.Equal("hey_jarvis", model.Name);
        Assert.Equal("/models/hey_jarvis.tflite", model.FilePath);
    }

    [Fact]
    public void GetModel_ComplexPath_ExtractsFilenameCorrectly()
    {
        // Arrange
        var provider = new FileBasedModelProvider();
        var complexPath = "/home/user/voice-assistant/models/wake-words/alexa_v0.1_t0.7.onnx";

        // Act
        var model = provider.GetModel(complexPath);

        // Assert
        Assert.Equal("alexa_v0.1_t0.7", model.Name);
        Assert.Equal(complexPath, model.FilePath);
        Assert.Equal("0.1", model.Version);
        Assert.Equal(0.7f, model.Threshold);
    }
}
