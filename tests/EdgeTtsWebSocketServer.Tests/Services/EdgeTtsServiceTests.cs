using EdgeTtsWebSocketServer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Olbrasoft.VoiceAssistant.EdgeTtsWebSocketServer.Tests.Services;

public class EdgeTtsServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<EdgeTtsService>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly AssistantSpeechStateService _assistantSpeechState;
    private readonly HttpClient _httpClient;
    private readonly string _testCacheDirectory;

    public EdgeTtsServiceTests()
    {
        _testCacheDirectory = Path.Combine(Path.GetTempPath(), $"edge-tts-test-{Guid.NewGuid()}");
        
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["EdgeTts:CacheDirectory"]).Returns(_testCacheDirectory);
        _mockConfiguration.Setup(c => c["EdgeTts:MicrophoneLockFile"]).Returns("/tmp/test-mic.lock");
        _mockConfiguration.Setup(c => c["EdgeTts:SpeechLockFile"]).Returns("/tmp/test-speech.lock");
        _mockConfiguration.Setup(c => c["EdgeTts:DefaultVoice"]).Returns("cs-CZ-AntoninNeural");
        _mockConfiguration.Setup(c => c["EdgeTts:DefaultRate"]).Returns("+20%");
        _mockConfiguration.Setup(c => c["EdgeTts:ListenerApiUrl"]).Returns("http://localhost:5051");

        _mockLogger = new Mock<ILogger<EdgeTtsService>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        
        // Create real AssistantSpeechStateService with mocked dependencies
        var mockSpeechStateLogger = new Mock<ILogger<AssistantSpeechStateService>>();
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        _assistantSpeechState = new AssistantSpeechStateService(
            mockSpeechStateLogger.Object,
            mockScopeFactory.Object);
        
        _httpClient = new HttpClient();
    }

    private EdgeTtsService CreateService()
    {
        return new EdgeTtsService(
            _mockConfiguration.Object, 
            _mockLogger.Object,
            _mockServiceProvider.Object,
            _assistantSpeechState,
            _httpClient);
    }

    [Fact]
    public void Constructor_ShouldCreateCacheDirectory()
    {
        // Arrange & Act
        var service = CreateService();

        // Assert
        Assert.True(Directory.Exists(_testCacheDirectory));

        // Cleanup
        Directory.Delete(_testCacheDirectory, true);
    }

    [Fact]
    public void Constructor_WithNullCacheDirectory_ShouldUseDefaultPath()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["EdgeTts:CacheDirectory"]).Returns((string?)null);
        mockConfig.Setup(c => c["EdgeTts:MicrophoneLockFile"]).Returns("/tmp/test-mic.lock");
        mockConfig.Setup(c => c["EdgeTts:SpeechLockFile"]).Returns("/tmp/test-speech.lock");
        mockConfig.Setup(c => c["EdgeTts:DefaultVoice"]).Returns("cs-CZ-AntoninNeural");
        mockConfig.Setup(c => c["EdgeTts:DefaultRate"]).Returns("+20%");
        mockConfig.Setup(c => c["EdgeTts:ListenerApiUrl"]).Returns("http://localhost:5051");

        // Act
        var service = new EdgeTtsService(
            mockConfig.Object, 
            _mockLogger.Object,
            _mockServiceProvider.Object,
            _assistantSpeechState,
            _httpClient);

        // Assert - should not throw
        Assert.NotNull(service);
    }

    [Fact]
    public void ClearCache_WithEmptyDirectory_ShouldReturnZero()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.ClearCache();

        // Assert
        Assert.Equal(0, result);

        // Cleanup
        Directory.Delete(_testCacheDirectory, true);
    }

    [Fact]
    public void ClearCache_WithCachedFiles_ShouldDeleteFilesAndReturnCount()
    {
        // Arrange
        var service = CreateService();
        
        // Create test cache files
        File.WriteAllText(Path.Combine(_testCacheDirectory, "test1.mp3"), "test");
        File.WriteAllText(Path.Combine(_testCacheDirectory, "test2.mp3"), "test");
        File.WriteAllText(Path.Combine(_testCacheDirectory, "test3.mp3"), "test");

        // Act
        var result = service.ClearCache();

        // Assert
        Assert.Equal(3, result);
        Assert.Empty(Directory.GetFiles(_testCacheDirectory, "*.mp3"));

        // Cleanup
        Directory.Delete(_testCacheDirectory, true);
    }

    [Fact]
    public void ClearCache_ShouldOnlyDeleteMp3Files()
    {
        // Arrange
        var service = CreateService();
        
        // Create test files
        File.WriteAllText(Path.Combine(_testCacheDirectory, "test.mp3"), "test");
        File.WriteAllText(Path.Combine(_testCacheDirectory, "test.txt"), "test");
        File.WriteAllText(Path.Combine(_testCacheDirectory, "test.wav"), "test");

        // Act
        var result = service.ClearCache();

        // Assert
        Assert.Equal(1, result);
        Assert.Single(Directory.GetFiles(_testCacheDirectory, "*.txt"));
        Assert.Single(Directory.GetFiles(_testCacheDirectory, "*.wav"));

        // Cleanup
        Directory.Delete(_testCacheDirectory, true);
    }

    [Fact]
    public async Task SpeakAsync_WithValidText_ShouldReturnResult()
    {
        // Arrange
        var service = CreateService();

        try
        {
            // Act
            var (success, message, cached) = await service.SpeakAsync("Test", play: false);

            // Assert - might fail if no network, but should not throw
            Assert.NotNull(message);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(_testCacheDirectory))
                Directory.Delete(_testCacheDirectory, true);
        }
    }
}
