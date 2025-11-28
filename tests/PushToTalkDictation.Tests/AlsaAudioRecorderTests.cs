using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Olbrasoft.VoiceAssistant.PushToTalkDictation.Tests;

public class AlsaAudioRecorderTests
{
    private readonly Mock<ILogger<AlsaAudioRecorder>> _mockLogger;

    public AlsaAudioRecorderTests()
    {
        _mockLogger = new Mock<ILogger<AlsaAudioRecorder>>();
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AlsaAudioRecorder(null!));
    }

    [Fact]
    public void Constructor_DefaultParameters_CreatesInstanceWithCorrectSettings()
    {
        // Arrange & Act
        var recorder = new AlsaAudioRecorder(_mockLogger.Object);

        // Assert
        Assert.NotNull(recorder);
        Assert.Equal(16000, recorder.SampleRate);
        Assert.Equal(1, recorder.Channels);
        Assert.Equal(16, recorder.BitsPerSample);
        Assert.False(recorder.IsRecording);
    }

    [Fact]
    public void Constructor_CustomParameters_CreatesInstanceWithCorrectSettings()
    {
        // Arrange & Act
        var recorder = new AlsaAudioRecorder(
            _mockLogger.Object,
            sampleRate: 44100,
            channels: 2,
            bitsPerSample: 24,
            deviceName: "hw:1,0");

        // Assert
        Assert.Equal(44100, recorder.SampleRate);
        Assert.Equal(2, recorder.Channels);
        Assert.Equal(24, recorder.BitsPerSample);
    }

    [Fact]
    public void IsRecording_InitialState_ReturnsFalse()
    {
        // Arrange
        var recorder = new AlsaAudioRecorder(_mockLogger.Object);

        // Act & Assert
        Assert.False(recorder.IsRecording);
    }

    [Fact]
    public void GetRecordedData_NoRecording_ReturnsEmptyArray()
    {
        // Arrange
        var recorder = new AlsaAudioRecorder(_mockLogger.Object);

        // Act
        var data = recorder.GetRecordedData();

        // Assert
        Assert.NotNull(data);
        Assert.Empty(data);
    }

    [Fact]
    public async Task StopRecordingAsync_NotRecording_LogsWarning()
    {
        // Arrange
        var recorder = new AlsaAudioRecorder(_mockLogger.Object);

        // Act
        await recorder.StopRecordingAsync();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not active")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        // Arrange
        var recorder = new AlsaAudioRecorder(_mockLogger.Object);

        // Act & Assert
        recorder.Dispose();
        recorder.Dispose(); // Should not throw
    }

    [Fact]
    public async Task Dispose_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var recorder = new AlsaAudioRecorder(_mockLogger.Object);
        recorder.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await recorder.StartRecordingAsync());
    }
}
