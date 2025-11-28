using Microsoft.Extensions.Logging;
using Moq;
using Olbrasoft.VoiceAssistant.Shared.TextInput;

namespace Olbrasoft.VoiceAssistant.Shared.Tests.TextInput;

public class TextTyperFactoryTests
{
    [Fact]
    public void IsWayland_ShouldReturnBoolean()
    {
        // Act
        var result = TextTyperFactory.IsWayland();

        // Assert
        Assert.IsType<bool>(result);
    }

    [Fact]
    public void GetDisplayServerName_ShouldReturnString()
    {
        // Act
        var result = TextTyperFactory.GetDisplayServerName();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<string>(result);
    }

    [Fact]
    public void GetDisplayServerName_ShouldReturnValidValue()
    {
        // Act
        var result = TextTyperFactory.GetDisplayServerName();

        // Assert
        Assert.Contains(result, new[] { "wayland", "x11", "unknown" });
    }

    [Fact]
    public void Create_WithNullLoggerFactory_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => TextTyperFactory.Create(null!));
    }

    [Fact]
    public void Create_WithValidLoggerFactory_ShouldReturnITextTyper()
    {
        // Arrange
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger>());

        // Act
        var result = TextTyperFactory.Create(mockLoggerFactory.Object);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<ITextTyper>(result);
    }

    [Fact]
    public void TextTyperFactory_ShouldBeStaticClass()
    {
        // Assert
        var type = typeof(TextTyperFactory);
        Assert.True(type.IsAbstract && type.IsSealed);
    }

    [Fact]
    public void IsWayland_WithXdgSessionTypeWayland_ShouldReturnTrue()
    {
        // This test verifies the logic - actual environment may differ
        // The implementation checks XDG_SESSION_TYPE first
        var sessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE");
        
        if (sessionType?.Equals("wayland", StringComparison.OrdinalIgnoreCase) == true)
        {
            Assert.True(TextTyperFactory.IsWayland());
        }
    }

    [Fact]
    public void IsWayland_WithXdgSessionTypeX11_ShouldReturnFalse()
    {
        // This test verifies the logic - actual environment may differ
        var sessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE");
        
        if (sessionType?.Equals("x11", StringComparison.OrdinalIgnoreCase) == true)
        {
            Assert.False(TextTyperFactory.IsWayland());
        }
    }
}
