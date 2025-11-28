using EdgeTtsWebSocketServer.Models;

namespace Olbrasoft.VoiceAssistant.EdgeTtsWebSocketServer.Tests.Models;

public class SpeechRequestTests
{
    [Fact]
    public void Text_DefaultValue_ShouldBeEmptyString()
    {
        // Arrange & Act
        var request = new SpeechRequest();

        // Assert
        Assert.Equal(string.Empty, request.Text);
    }

    [Fact]
    public void Voice_DefaultValue_ShouldBeNull()
    {
        // Arrange & Act
        var request = new SpeechRequest();

        // Assert
        Assert.Null(request.Voice);
    }

    [Fact]
    public void Rate_DefaultValue_ShouldBeNull()
    {
        // Arrange & Act
        var request = new SpeechRequest();

        // Assert
        Assert.Null(request.Rate);
    }

    [Fact]
    public void Volume_DefaultValue_ShouldBeNull()
    {
        // Arrange & Act
        var request = new SpeechRequest();

        // Assert
        Assert.Null(request.Volume);
    }

    [Fact]
    public void Pitch_DefaultValue_ShouldBeNull()
    {
        // Arrange & Act
        var request = new SpeechRequest();

        // Assert
        Assert.Null(request.Pitch);
    }

    [Fact]
    public void Play_DefaultValue_ShouldBeTrue()
    {
        // Arrange & Act
        var request = new SpeechRequest();

        // Assert
        Assert.True(request.Play);
    }

    [Fact]
    public void Properties_WhenSet_ShouldReturnSetValues()
    {
        // Arrange
        var request = new SpeechRequest
        {
            Text = "Hello World",
            Voice = "cs-CZ-AntoninNeural",
            Rate = "+20%",
            Volume = "+10%",
            Pitch = "+5Hz",
            Play = false
        };

        // Assert
        Assert.Equal("Hello World", request.Text);
        Assert.Equal("cs-CZ-AntoninNeural", request.Voice);
        Assert.Equal("+20%", request.Rate);
        Assert.Equal("+10%", request.Volume);
        Assert.Equal("+5Hz", request.Pitch);
        Assert.False(request.Play);
    }
}
