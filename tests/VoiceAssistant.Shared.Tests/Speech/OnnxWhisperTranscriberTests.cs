using Microsoft.Extensions.Logging;
using Moq;
using Olbrasoft.VoiceAssistant.Shared.Speech;
using Xunit;

namespace Olbrasoft.VoiceAssistant.Shared.Tests.Speech;

public class OnnxWhisperTranscriberTests
{
    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new OnnxWhisperTranscriber(null!, "/path/to/model.onnx"));
    }

    [Fact]
    public void Constructor_WithNullModelPath_ThrowsArgumentNullException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<OnnxWhisperTranscriber>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new OnnxWhisperTranscriber(loggerMock.Object, null!));
    }

    [Fact]
    public void Constructor_WithNonExistentModelPath_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<OnnxWhisperTranscriber>>();
        var nonExistentPath = "/path/to/nonexistent/model";

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() => 
            new OnnxWhisperTranscriber(loggerMock.Object, nonExistentPath));
    }

    [Fact]
    public void Language_ReturnsConfiguredLanguage()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<OnnxWhisperTranscriber>>();
        var tempModelDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempModelDir);
        
        try
        {
            var transcriber = new OnnxWhisperTranscriber(loggerMock.Object, tempModelDir, "en");

            // Act
            var language = transcriber.Language;

            // Assert
            Assert.Equal("en", language);
        }
        finally
        {
            Directory.Delete(tempModelDir, recursive: true);
        }
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<OnnxWhisperTranscriber>>();
        var tempModelDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempModelDir);
        
        try
        {
            var transcriber = new OnnxWhisperTranscriber(loggerMock.Object, tempModelDir);

            // Act & Assert - should not throw
            transcriber.Dispose();
            transcriber.Dispose();
        }
        finally
        {
            Directory.Delete(tempModelDir, recursive: true);
        }
    }
}
