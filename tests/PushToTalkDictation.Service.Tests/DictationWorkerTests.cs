using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Olbrasoft.VoiceAssistant.PushToTalkDictation;
using Olbrasoft.VoiceAssistant.Shared.Speech;
using Olbrasoft.VoiceAssistant.Shared.TextInput;

namespace Olbrasoft.VoiceAssistant.PushToTalkDictation.Service.Tests;

public class DictationWorkerTests
{
    private readonly Mock<ILogger<DictationWorker>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IKeyboardMonitor> _mockKeyboardMonitor;
    private readonly Mock<IAudioRecorder> _mockAudioRecorder;
    private readonly Mock<ISpeechTranscriber> _mockSpeechTranscriber;
    private readonly Mock<ITextTyper> _mockTextTyper;

    public DictationWorkerTests()
    {
        _mockLogger = new Mock<ILogger<DictationWorker>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockKeyboardMonitor = new Mock<IKeyboardMonitor>();
        _mockAudioRecorder = new Mock<IAudioRecorder>();
        _mockSpeechTranscriber = new Mock<ISpeechTranscriber>();
        _mockTextTyper = new Mock<ITextTyper>();

        // Default configuration
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Value).Returns("CapsLock");
        _mockConfiguration.Setup(c => c.GetSection("PushToTalkDictation:TriggerKey")).Returns(mockSection.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DictationWorker(
            null!,
            _mockConfiguration.Object,
            _mockKeyboardMonitor.Object,
            _mockAudioRecorder.Object,
            _mockSpeechTranscriber.Object,
            _mockTextTyper.Object));
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DictationWorker(
            _mockLogger.Object,
            null!,
            _mockKeyboardMonitor.Object,
            _mockAudioRecorder.Object,
            _mockSpeechTranscriber.Object,
            _mockTextTyper.Object));
    }

    [Fact]
    public void Constructor_WithNullKeyboardMonitor_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DictationWorker(
            _mockLogger.Object,
            _mockConfiguration.Object,
            null!,
            _mockAudioRecorder.Object,
            _mockSpeechTranscriber.Object,
            _mockTextTyper.Object));
    }

    [Fact]
    public void Constructor_WithNullAudioRecorder_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DictationWorker(
            _mockLogger.Object,
            _mockConfiguration.Object,
            _mockKeyboardMonitor.Object,
            null!,
            _mockSpeechTranscriber.Object,
            _mockTextTyper.Object));
    }

    [Fact]
    public void Constructor_WithNullSpeechTranscriber_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DictationWorker(
            _mockLogger.Object,
            _mockConfiguration.Object,
            _mockKeyboardMonitor.Object,
            _mockAudioRecorder.Object,
            null!,
            _mockTextTyper.Object));
    }

    [Fact]
    public void Constructor_WithNullTextTyper_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DictationWorker(
            _mockLogger.Object,
            _mockConfiguration.Object,
            _mockKeyboardMonitor.Object,
            _mockAudioRecorder.Object,
            _mockSpeechTranscriber.Object,
            null!));
    }

    [Fact]
    public void Constructor_WithValidDependencies_ShouldCreateInstance()
    {
        // Act
        var worker = new DictationWorker(
            _mockLogger.Object,
            _mockConfiguration.Object,
            _mockKeyboardMonitor.Object,
            _mockAudioRecorder.Object,
            _mockSpeechTranscriber.Object,
            _mockTextTyper.Object);

        // Assert
        Assert.NotNull(worker);
    }

    [Fact]
    public void Constructor_ShouldReadTriggerKeyFromConfiguration()
    {
        // Arrange
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Value).Returns("ScrollLock");
        _mockConfiguration.Setup(c => c.GetSection("PushToTalkDictation:TriggerKey")).Returns(mockSection.Object);

        // Act - should not throw even with different trigger key
        var worker = new DictationWorker(
            _mockLogger.Object,
            _mockConfiguration.Object,
            _mockKeyboardMonitor.Object,
            _mockAudioRecorder.Object,
            _mockSpeechTranscriber.Object,
            _mockTextTyper.Object);

        // Assert
        Assert.NotNull(worker);
    }

    [Fact]
    public async Task StopAsync_WhenNotRecording_ShouldStopMonitoring()
    {
        // Arrange
        var worker = new DictationWorker(
            _mockLogger.Object,
            _mockConfiguration.Object,
            _mockKeyboardMonitor.Object,
            _mockAudioRecorder.Object,
            _mockSpeechTranscriber.Object,
            _mockTextTyper.Object);

        _mockKeyboardMonitor.Setup(m => m.StopMonitoringAsync())
            .Returns(Task.CompletedTask);

        // Act
        await worker.StopAsync(CancellationToken.None);

        // Assert
        _mockKeyboardMonitor.Verify(m => m.StopMonitoringAsync(), Times.Once);
    }

    [Fact]
    public void Worker_ShouldBeBackgroundService()
    {
        // Arrange & Act
        var worker = new DictationWorker(
            _mockLogger.Object,
            _mockConfiguration.Object,
            _mockKeyboardMonitor.Object,
            _mockAudioRecorder.Object,
            _mockSpeechTranscriber.Object,
            _mockTextTyper.Object);

        // Assert
        Assert.IsAssignableFrom<BackgroundService>(worker);
    }
}
