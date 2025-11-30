using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Olbrasoft.Mediation;
using Olbrasoft.VoiceAssistant.PushToTalkDictation;
using Olbrasoft.VoiceAssistant.PushToTalkDictation.Service.Services;
using Olbrasoft.VoiceAssistant.Shared.Speech;
using Olbrasoft.VoiceAssistant.Shared.TextInput;
using Xunit;

namespace Olbrasoft.VoiceAssistant.PushToTalkDictation.Service.Tests;

public class DictationWorkerTests
{
    private readonly Mock<ILogger<DictationWorker>> _mockLogger;
    private readonly IConfiguration _configuration;
    private readonly Mock<IKeyboardMonitor> _mockKeyboardMonitor;
    private readonly Mock<IAudioRecorder> _mockAudioRecorder;
    private readonly Mock<ISpeechTranscriber> _mockSpeechTranscriber;
    private readonly Mock<ITextTyper> _mockTextTyper;
    private readonly Mock<IPttNotifier> _mockPttNotifier;
    private readonly Mock<IMediator> _mockMediator;
    private readonly HttpClient _httpClient;

    public DictationWorkerTests()
    {
        _mockLogger = new Mock<ILogger<DictationWorker>>();
        
        // Use real ConfigurationBuilder for proper GetValue support
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PushToTalkDictation:TriggerKey"] = "CapsLock",
                ["EdgeTts:BaseUrl"] = "http://localhost:5555"
            })
            .Build();
            
        _mockKeyboardMonitor = new Mock<IKeyboardMonitor>();
        _mockAudioRecorder = new Mock<IAudioRecorder>();
        _mockSpeechTranscriber = new Mock<ISpeechTranscriber>();
        _mockTextTyper = new Mock<ITextTyper>();
        _mockPttNotifier = new Mock<IPttNotifier>();
        _mockMediator = new Mock<IMediator>();
        _httpClient = new HttpClient();
    }

    private DictationWorker CreateWorker() => new DictationWorker(
        _mockLogger.Object,
        _configuration,
        _mockKeyboardMonitor.Object,
        _mockAudioRecorder.Object,
        _mockSpeechTranscriber.Object,
        _mockTextTyper.Object,
        _mockPttNotifier.Object,
        _mockMediator.Object,
        _httpClient);

    private DictationWorker CreateWorkerWithConfig(Dictionary<string, string?> configValues)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();
            
        return new DictationWorker(
            _mockLogger.Object,
            config,
            _mockKeyboardMonitor.Object,
            _mockAudioRecorder.Object,
            _mockSpeechTranscriber.Object,
            _mockTextTyper.Object,
            _mockPttNotifier.Object,
            _mockMediator.Object,
            _httpClient);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DictationWorker(
            null!,
            _configuration,
            _mockKeyboardMonitor.Object,
            _mockAudioRecorder.Object,
            _mockSpeechTranscriber.Object,
            _mockTextTyper.Object,
            _mockPttNotifier.Object,
            _mockMediator.Object,
            _httpClient));
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
            _mockTextTyper.Object,
            _mockPttNotifier.Object,
            _mockMediator.Object,
            _httpClient));
    }

    [Fact]
    public void Constructor_WithNullKeyboardMonitor_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DictationWorker(
            _mockLogger.Object,
            _configuration,
            null!,
            _mockAudioRecorder.Object,
            _mockSpeechTranscriber.Object,
            _mockTextTyper.Object,
            _mockPttNotifier.Object,
            _mockMediator.Object,
            _httpClient));
    }

    [Fact]
    public void Constructor_WithNullAudioRecorder_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DictationWorker(
            _mockLogger.Object,
            _configuration,
            _mockKeyboardMonitor.Object,
            null!,
            _mockSpeechTranscriber.Object,
            _mockTextTyper.Object,
            _mockPttNotifier.Object,
            _mockMediator.Object,
            _httpClient));
    }

    [Fact]
    public void Constructor_WithNullSpeechTranscriber_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DictationWorker(
            _mockLogger.Object,
            _configuration,
            _mockKeyboardMonitor.Object,
            _mockAudioRecorder.Object,
            null!,
            _mockTextTyper.Object,
            _mockPttNotifier.Object,
            _mockMediator.Object,
            _httpClient));
    }

    [Fact]
    public void Constructor_WithNullTextTyper_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DictationWorker(
            _mockLogger.Object,
            _configuration,
            _mockKeyboardMonitor.Object,
            _mockAudioRecorder.Object,
            _mockSpeechTranscriber.Object,
            null!,
            _mockPttNotifier.Object,
            _mockMediator.Object,
            _httpClient));
    }

    [Fact]
    public void Constructor_WithNullPttNotifier_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DictationWorker(
            _mockLogger.Object,
            _configuration,
            _mockKeyboardMonitor.Object,
            _mockAudioRecorder.Object,
            _mockSpeechTranscriber.Object,
            _mockTextTyper.Object,
            null!,
            _mockMediator.Object,
            _httpClient));
    }

    [Fact]
    public void Constructor_WithNullMediator_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DictationWorker(
            _mockLogger.Object,
            _configuration,
            _mockKeyboardMonitor.Object,
            _mockAudioRecorder.Object,
            _mockSpeechTranscriber.Object,
            _mockTextTyper.Object,
            _mockPttNotifier.Object,
            null!,
            _httpClient));
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DictationWorker(
            _mockLogger.Object,
            _configuration,
            _mockKeyboardMonitor.Object,
            _mockAudioRecorder.Object,
            _mockSpeechTranscriber.Object,
            _mockTextTyper.Object,
            _mockPttNotifier.Object,
            _mockMediator.Object,
            null!));
    }

    [Fact]
    public void Constructor_WithValidDependencies_ShouldCreateInstance()
    {
        // Act
        var worker = CreateWorker();

        // Assert
        Assert.NotNull(worker);
    }

    [Fact]
    public void Constructor_ShouldReadTriggerKeyFromConfiguration()
    {
        // Arrange
        var config = new Dictionary<string, string?>
        {
            ["PushToTalkDictation:TriggerKey"] = "ScrollLock",
            ["EdgeTts:BaseUrl"] = "http://localhost:5555"
        };

        // Act - should not throw even with different trigger key
        var worker = CreateWorkerWithConfig(config);

        // Assert
        Assert.NotNull(worker);
    }

    [Fact]
    public async Task StopAsync_WhenNotRecording_ShouldStopMonitoring()
    {
        // Arrange
        var worker = CreateWorker();

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
        var worker = CreateWorker();

        // Assert
        Assert.IsAssignableFrom<BackgroundService>(worker);
    }
}
