using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Olbrasoft.VoiceAssistant.WakeWordDetection.Tests;

public class OpenWakeWordDetectorTests
{
    private readonly Mock<ILogger<OpenWakeWordDetector>> _loggerMock;
    private readonly Mock<IAudioCapture> _audioCaptureMock;
    private readonly Mock<IWakeWordModelProvider> _modelProviderMock;
    private readonly string _testModelsPath;

    public OpenWakeWordDetectorTests()
    {
        _loggerMock = new Mock<ILogger<OpenWakeWordDetector>>();
        _audioCaptureMock = new Mock<IAudioCapture>();
        _modelProviderMock = new Mock<IWakeWordModelProvider>();
        
        // Use actual model paths from the project
        _testModelsPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "WakeWordListener", "Models");
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitialize()
    {
        // Arrange
        var wakeWordModelPath = Path.Combine(_testModelsPath, "hey_jarvis_v0.1_t0.35.onnx");
        var melspecModelPath = Path.Combine(_testModelsPath, "melspectrogram.onnx");
        var embeddingModelPath = Path.Combine(_testModelsPath, "embedding_model.onnx");

        // Skip test if models don't exist (CI environment)
        if (!File.Exists(wakeWordModelPath) || !File.Exists(melspecModelPath) || !File.Exists(embeddingModelPath))
        {
            return;
        }
        
        // Setup model provider
        _modelProviderMock.Setup(p => p.GetModels(It.IsAny<IEnumerable<string>>()))
            .Returns(new List<WakeWordModel>
            {
                new WakeWordModel
                {
                    Name = "hey_jarvis_v0.1_t0.35",
                    FilePath = wakeWordModelPath,
                    Threshold = 0.35f,
                    HasExplicitThreshold = true
                }
            });

        // Act & Assert
        var detector = new OpenWakeWordDetector(
            _loggerMock.Object,
            _audioCaptureMock.Object,
            _modelProviderMock.Object,
            new List<string> { wakeWordModelPath },
            melspecModelPath,
            embeddingModelPath,
            debounceSeconds: 2.0);

        Assert.NotNull(detector);
        Assert.False(detector.IsListening);
        
        detector.Dispose();
    }

    [Fact]
    public void GetWakeWords_ShouldReturnHeyJarvis()
    {
        // Arrange
        var wakeWordModelPath = Path.Combine(_testModelsPath, "hey_jarvis_v0.1_t0.35.onnx");
        var melspecModelPath = Path.Combine(_testModelsPath, "melspectrogram.onnx");
        var embeddingModelPath = Path.Combine(_testModelsPath, "embedding_model.onnx");

        // Skip test if models don't exist
        if (!File.Exists(wakeWordModelPath) || !File.Exists(melspecModelPath) || !File.Exists(embeddingModelPath))
        {
            return;
        }
        
        // Setup model provider
        _modelProviderMock.Setup(p => p.GetModels(It.IsAny<IEnumerable<string>>()))
            .Returns(new List<WakeWordModel>
            {
                new WakeWordModel
                {
                    Name = "hey_jarvis_v0.1_t0.35",
                    FilePath = wakeWordModelPath,
                    Threshold = 0.35f,
                    HasExplicitThreshold = true
                }
            });

        var detector = new OpenWakeWordDetector(
            _loggerMock.Object,
            _audioCaptureMock.Object,
            _modelProviderMock.Object,
            new List<string> { wakeWordModelPath },
            melspecModelPath,
            embeddingModelPath);

        // Act
        var wakeWords = detector.GetWakeWords();

        // Assert
        Assert.NotNull(wakeWords);
        Assert.Single(wakeWords);
        Assert.Contains("hey_jarvis_v0.1_t0.35", wakeWords);
        
        detector.Dispose();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var wakeWordModelPath = "test.onnx";
        var melspecModelPath = "test2.onnx";
        var embeddingModelPath = "test3.onnx";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OpenWakeWordDetector(
            null!,
            _audioCaptureMock.Object,
            _modelProviderMock.Object,
            new List<string> { wakeWordModelPath },
            melspecModelPath,
            embeddingModelPath));
    }

    [Fact]
    public void Constructor_WithNullAudioCapture_ShouldThrowArgumentNullException()
    {
        // Arrange
        var wakeWordModelPath = "test.onnx";
        var melspecModelPath = "test2.onnx";
        var embeddingModelPath = "test3.onnx";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OpenWakeWordDetector(
            _loggerMock.Object,
            null!,
            _modelProviderMock.Object,
            new List<string> { wakeWordModelPath },
            melspecModelPath,
            embeddingModelPath));
    }

    [Fact]
    public void Dispose_ShouldBeIdempotent()
    {
        // Arrange
        var wakeWordModelPath = Path.Combine(_testModelsPath, "hey_jarvis_v0.1_t0.35.onnx");
        var melspecModelPath = Path.Combine(_testModelsPath, "melspectrogram.onnx");
        var embeddingModelPath = Path.Combine(_testModelsPath, "embedding_model.onnx");

        // Skip test if models don't exist
        if (!File.Exists(wakeWordModelPath) || !File.Exists(melspecModelPath) || !File.Exists(embeddingModelPath))
        {
            return;
        }
        
        // Setup model provider
        _modelProviderMock.Setup(p => p.GetModels(It.IsAny<IEnumerable<string>>()))
            .Returns(new List<WakeWordModel>
            {
                new WakeWordModel
                {
                    Name = "hey_jarvis_v0.1_t0.35",
                    FilePath = wakeWordModelPath,
                    Threshold = 0.35f,
                    HasExplicitThreshold = true
                }
            });

        var detector = new OpenWakeWordDetector(
            _loggerMock.Object,
            _audioCaptureMock.Object,
            _modelProviderMock.Object,
            new List<string> { wakeWordModelPath },
            melspecModelPath,
            embeddingModelPath);

        // Act & Assert - should not throw
        detector.Dispose();
        detector.Dispose();
        detector.Dispose();
    }
}
