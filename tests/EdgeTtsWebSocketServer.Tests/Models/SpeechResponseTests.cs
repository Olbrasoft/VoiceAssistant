using EdgeTtsWebSocketServer.Models;
using Xunit;

namespace Olbrasoft.VoiceAssistant.EdgeTtsWebSocketServer.Tests.Models;

public class SpeechResponseTests
{
    [Fact]
    public void Success_DefaultValue_ShouldBeFalse()
    {
        // Arrange & Act
        var response = new SpeechResponse();

        // Assert
        Assert.False(response.Success);
    }

    [Fact]
    public void Message_DefaultValue_ShouldBeEmptyString()
    {
        // Arrange & Act
        var response = new SpeechResponse();

        // Assert
        Assert.Equal(string.Empty, response.Message);
    }

    [Fact]
    public void Cached_DefaultValue_ShouldBeFalse()
    {
        // Arrange & Act
        var response = new SpeechResponse();

        // Assert
        Assert.False(response.Cached);
    }

    [Fact]
    public void Properties_WhenSet_ShouldReturnSetValues()
    {
        // Arrange
        var response = new SpeechResponse
        {
            Success = true,
            Message = "Audio generated successfully",
            Cached = true
        };

        // Assert
        Assert.True(response.Success);
        Assert.Equal("Audio generated successfully", response.Message);
        Assert.True(response.Cached);
    }

    [Fact]
    public void Success_WhenSetToTrue_ShouldBeTrue()
    {
        // Arrange
        var response = new SpeechResponse { Success = true };

        // Assert
        Assert.True(response.Success);
    }

    [Fact]
    public void Cached_WhenSetToTrue_ShouldBeTrue()
    {
        // Arrange
        var response = new SpeechResponse { Cached = true };

        // Assert
        Assert.True(response.Cached);
    }
}
