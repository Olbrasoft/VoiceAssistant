namespace Olbrasoft.VoiceAssistant.PushToTalkDictation.Tests;

public class KeyCodeTests
{
    [Fact]
    public void Unknown_ShouldHaveValueZero()
    {
        // Assert
        Assert.Equal(0, (int)KeyCode.Unknown);
    }

    [Fact]
    public void Escape_ShouldHaveValueOne()
    {
        // Assert
        Assert.Equal(1, (int)KeyCode.Escape);
    }

    [Fact]
    public void CapsLock_ShouldHaveValue58()
    {
        // Assert
        Assert.Equal(58, (int)KeyCode.CapsLock);
    }

    [Fact]
    public void ScrollLock_ShouldHaveValue70()
    {
        // Assert
        Assert.Equal(70, (int)KeyCode.ScrollLock);
    }

    [Fact]
    public void NumLock_ShouldHaveValue69()
    {
        // Assert
        Assert.Equal(69, (int)KeyCode.NumLock);
    }

    [Fact]
    public void LeftControl_ShouldHaveValue29()
    {
        // Assert
        Assert.Equal(29, (int)KeyCode.LeftControl);
    }

    [Fact]
    public void RightControl_ShouldHaveValue97()
    {
        // Assert
        Assert.Equal(97, (int)KeyCode.RightControl);
    }

    [Fact]
    public void LeftShift_ShouldHaveValue42()
    {
        // Assert
        Assert.Equal(42, (int)KeyCode.LeftShift);
    }

    [Fact]
    public void RightShift_ShouldHaveValue54()
    {
        // Assert
        Assert.Equal(54, (int)KeyCode.RightShift);
    }

    [Fact]
    public void LeftAlt_ShouldHaveValue56()
    {
        // Assert
        Assert.Equal(56, (int)KeyCode.LeftAlt);
    }

    [Fact]
    public void RightAlt_ShouldHaveValue100()
    {
        // Assert
        Assert.Equal(100, (int)KeyCode.RightAlt);
    }

    [Fact]
    public void Space_ShouldHaveValue57()
    {
        // Assert
        Assert.Equal(57, (int)KeyCode.Space);
    }

    [Fact]
    public void Enter_ShouldHaveValue28()
    {
        // Assert
        Assert.Equal(28, (int)KeyCode.Enter);
    }

    [Fact]
    public void AllDefinedValues_ShouldBeUnique()
    {
        // Arrange
        var values = Enum.GetValues<KeyCode>().Cast<int>().ToList();

        // Act & Assert
        Assert.Equal(values.Count, values.Distinct().Count());
    }

    [Fact]
    public void CanParseFromInt_ShouldReturnCorrectKeyCode()
    {
        // Arrange & Act
        var capsLock = (KeyCode)58;
        var scrollLock = (KeyCode)70;

        // Assert
        Assert.Equal(KeyCode.CapsLock, capsLock);
        Assert.Equal(KeyCode.ScrollLock, scrollLock);
    }

    [Fact]
    public void IsDefined_ShouldReturnTrueForValidKeyCodes()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(KeyCode), 58)); // CapsLock
        Assert.True(Enum.IsDefined(typeof(KeyCode), 70)); // ScrollLock
        Assert.True(Enum.IsDefined(typeof(KeyCode), 0));  // Unknown
    }

    [Fact]
    public void IsDefined_ShouldReturnFalseForInvalidKeyCodes()
    {
        // Assert
        Assert.False(Enum.IsDefined(typeof(KeyCode), 999));
        Assert.False(Enum.IsDefined(typeof(KeyCode), -1));
    }
}
