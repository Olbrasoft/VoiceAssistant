using Xunit;

namespace Olbrasoft.VoiceAssistant.WakeWordDetection.Tests;

public class AlsaAudioCaptureTests
{
    [Fact]
    public void Constructor_DefaultParameters_SetsCorrectValues()
    {
        // Act
        using var capture = new AlsaAudioCapture();

        // Assert
        Assert.Equal(16000, capture.SampleRate);
        Assert.Equal(1, capture.Channels);
        Assert.Equal(16, capture.BitsPerSample);
        Assert.False(capture.IsCapturing);
    }

    [Theory]
    [InlineData(8000, 1, 16, 0)]
    [InlineData(16000, 1, 16, 0)]
    [InlineData(22050, 1, 16, 1)]
    [InlineData(44100, 2, 16, 2)]
    [InlineData(48000, 2, 24, 3)]
    public void Constructor_CustomParameters_SetsCorrectValues(
        int sampleRate, int channels, int bitsPerSample, int deviceNumber)
    {
        // Act
        using var capture = new AlsaAudioCapture(sampleRate, channels, bitsPerSample, deviceNumber);

        // Assert
        Assert.Equal(sampleRate, capture.SampleRate);
        Assert.Equal(channels, capture.Channels);
        Assert.Equal(bitsPerSample, capture.BitsPerSample);
        Assert.False(capture.IsCapturing);
    }

    [Fact]
    public void SampleRate_ReturnsConfiguredValue()
    {
        // Arrange
        using var capture = new AlsaAudioCapture(sampleRate: 22050);

        // Assert
        Assert.Equal(22050, capture.SampleRate);
    }

    [Fact]
    public void Channels_ReturnsConfiguredValue()
    {
        // Arrange
        using var capture = new AlsaAudioCapture(channels: 2);

        // Assert
        Assert.Equal(2, capture.Channels);
    }

    [Fact]
    public void BitsPerSample_ReturnsConfiguredValue()
    {
        // Arrange
        using var capture = new AlsaAudioCapture(bitsPerSample: 24);

        // Assert
        Assert.Equal(24, capture.BitsPerSample);
    }

    [Fact]
    public void IsCapturing_InitiallyFalse()
    {
        // Arrange
        using var capture = new AlsaAudioCapture();

        // Assert
        Assert.False(capture.IsCapturing);
    }

    [Fact]
    public void ImplementsIAudioCapture()
    {
        // Assert
        Assert.True(typeof(AlsaAudioCapture).GetInterfaces().Contains(typeof(IAudioCapture)));
    }

    [Fact]
    public void ImplementsIDisposable()
    {
        // Assert
        Assert.True(typeof(AlsaAudioCapture).GetInterfaces().Contains(typeof(IDisposable)));
    }

    [Fact]
    public void AudioDataAvailable_EventCanBeSubscribed()
    {
        // Arrange
        using var capture = new AlsaAudioCapture();
        var eventRaised = false;

        // Act
        capture.AudioDataAvailable += (sender, args) => eventRaised = true;

        // Assert - no exception thrown
        Assert.False(eventRaised); // Event not raised yet, just subscribed
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var capture = new AlsaAudioCapture();

        // Act & Assert - should not throw
        capture.Dispose();
        capture.Dispose();
    }

    [Fact]
    public async Task StopCaptureAsync_WhenNotCapturing_ReturnsImmediately()
    {
        // Arrange
        using var capture = new AlsaAudioCapture();

        // Act & Assert - should not throw
        await capture.StopCaptureAsync();
        Assert.False(capture.IsCapturing);
    }

    [Fact]
    public async Task StartCaptureAsync_ReturnsTask()
    {
        // Arrange
        var capture = new AlsaAudioCapture();
        
        try
        {
            // This test only verifies that the method exists and returns correctly
            // The actual capture may start pw-record process
            
            // Assert - method signature is correct and returns Task
            var task = capture.StartCaptureAsync();
            Assert.NotNull(task);
            
            // Give it a moment to start
            await Task.Delay(100);
        }
        finally
        {
            // CRITICAL: Always stop capture and dispose to prevent hanging processes
            await capture.StopCaptureAsync();
            capture.Dispose();
        }
    }
}
