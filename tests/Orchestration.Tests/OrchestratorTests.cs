using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Olbrasoft.VoiceAssistant.Orchestration.Services;

namespace Olbrasoft.VoiceAssistant.Orchestration.Tests;

public class OrchestratorTests
{
    private readonly Mock<ILogger<Orchestrator>> _mockLogger;
    private readonly Mock<SpeechRecognitionService> _mockSpeechRecognition;
    private readonly Mock<TextInputService> _mockTextInput;
    private readonly Mock<IConfiguration> _mockConfiguration;

    public OrchestratorTests()
    {
        _mockLogger = new Mock<ILogger<Orchestrator>>();
        _mockConfiguration = new Mock<IConfiguration>();
        
        // Setup default configuration
        _mockConfiguration.Setup(c => c["WakeWordServiceUrl"]).Returns("http://localhost:5000");
        _mockConfiguration.Setup(c => c.GetSection("OpenCodeAutoSubmit").Value).Returns("true");

        // Create mock dependencies for SpeechRecognitionService and TextInputService
        var mockSpeechLogger = new Mock<ILogger<SpeechRecognitionService>>();
        var mockTextInputLogger = new Mock<ILogger<TextInputService>>();
        
        _mockSpeechRecognition = new Mock<SpeechRecognitionService>(
            mockSpeechLogger.Object, 
            _mockConfiguration.Object);
        
        _mockTextInput = new Mock<TextInputService>(
            mockTextInputLogger.Object, 
            _mockConfiguration.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var orchestrator = new Orchestrator(
            _mockLogger.Object,
            _mockSpeechRecognition.Object,
            _mockTextInput.Object,
            _mockConfiguration.Object);

        // Assert
        Assert.NotNull(orchestrator);
    }

    [Fact]
    public void Orchestrator_ShouldImplementIOrchestrator()
    {
        // Arrange & Act
        var orchestrator = new Orchestrator(
            _mockLogger.Object,
            _mockSpeechRecognition.Object,
            _mockTextInput.Object,
            _mockConfiguration.Object);

        // Assert
        Assert.IsAssignableFrom<IOrchestrator>(orchestrator);
    }

    [Fact]
    public async Task StopAsync_WhenNotStarted_ShouldNotThrow()
    {
        // Arrange
        var orchestrator = new Orchestrator(
            _mockLogger.Object,
            _mockSpeechRecognition.Object,
            _mockTextInput.Object,
            _mockConfiguration.Object);

        // Act & Assert - should not throw
        await orchestrator.StopAsync(CancellationToken.None);
    }

    // NOTE: TriggerDictationAsync test removed - it plays audio and records from microphone,
    // which is not suitable for automated testing

    [Fact]
    public void IOrchestrator_ShouldDefineRequiredMethods()
    {
        // Arrange
        var type = typeof(IOrchestrator);

        // Assert
        Assert.NotNull(type.GetMethod(nameof(IOrchestrator.StartAsync)));
        Assert.NotNull(type.GetMethod(nameof(IOrchestrator.StopAsync)));
        Assert.NotNull(type.GetMethod(nameof(IOrchestrator.TriggerDictationAsync)));
    }
}
