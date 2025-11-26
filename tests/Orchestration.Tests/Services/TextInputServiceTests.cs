using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Olbrasoft.VoiceAssistant.Orchestration.Services;

namespace Orchestration.Tests.Services;

public class TextInputServiceTests
{
    private readonly Mock<ILogger<TextInputService>> _loggerMock;
    private readonly TextInputService _service;

    public TextInputServiceTests()
    {
        _loggerMock = new Mock<ILogger<TextInputService>>();
        _service = new TextInputService(_loggerMock.Object);
    }

    [Fact]
    public async Task TypeTextAsync_WithValidText_ReturnsTrue()
    {
        // Arrange
        var text = "Hello World";

        // Act
        var result = await _service.TypeTextAsync(text);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TypeTextAsync_WithEmptyText_ReturnsFalse()
    {
        // Arrange
        var text = "";

        // Act
        var result = await _service.TypeTextAsync(text);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TypeTextAsync_WithWhitespaceText_ReturnsFalse()
    {
        // Arrange
        var text = "   ";

        // Act
        var result = await _service.TypeTextAsync(text);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TypeTextAsync_WithNullText_ReturnsFalse()
    {
        // Arrange
        string? text = null;

        // Act
        var result = await _service.TypeTextAsync(text!);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("Simple text")]
    [InlineData("Text with numbers 123")]
    [InlineData("Text with special chars: !@#$%")]
    [InlineData("Česká diakritika: áčďěéíňóřšťúůýž")]
    public async Task TypeTextAsync_WithVariousTexts_LogsCorrectly(string text)
    {
        // Act
        await _service.TypeTextAsync(text);

        // Assert - verify logging happened
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Typing text")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
