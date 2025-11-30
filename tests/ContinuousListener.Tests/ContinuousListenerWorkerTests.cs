using Olbrasoft.VoiceAssistant.ContinuousListener;

namespace ContinuousListener.Tests;

/// <summary>
/// Unit tests for ContinuousListenerWorker methods.
/// </summary>
public class ContinuousListenerWorkerTests
{
    #region IsStopCommand Tests

    [Theory]
    [InlineData("stop", true)]
    [InlineData("Stop", true)]
    [InlineData("STOP", true)]
    [InlineData("stůj", true)]
    [InlineData("ticho", true)]
    [InlineData("dost", true)]
    [InlineData("přestaň", true)]
    [InlineData("zastav", true)]
    public void IsStopCommand_ExactMatch_ReturnsTrue(string text, bool expected)
    {
        // Act
        var result = ContinuousListenerWorker.IsStopCommand(text);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Počítači, stop", true)]
    [InlineData("počítači stop", true)]
    [InlineData("Počítači, prosím tě, stop", true)]
    [InlineData("Počítači, stop, stop, stop", true)]
    [InlineData("hej, stůj!", true)]
    [InlineData("prosím, ticho", true)]
    public void IsStopCommand_StopWordAnywhere_ReturnsTrue(string text, bool expected)
    {
        // Act
        var result = ContinuousListenerWorker.IsStopCommand(text);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("stop!", true)]
    [InlineData("stop.", true)]
    [InlineData("stop?", true)]
    [InlineData("stop, prosím", true)]
    [InlineData("  stop  ", true)]
    public void IsStopCommand_WithPunctuation_ReturnsTrue(string text, bool expected)
    {
        // Act
        var result = ContinuousListenerWorker.IsStopCommand(text);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void IsStopCommand_EmptyOrNull_ReturnsFalse(string? text, bool expected)
    {
        // Act
        var result = ContinuousListenerWorker.IsStopCommand(text!);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Počítači, řekni mi něco", false)]
    [InlineData("co je nového", false)]
    [InlineData("ahoj", false)]
    [InlineData("spusť příkaz", false)]
    [InlineData("zastopuj", false)]  // Contains "stop" but as part of another word
    [InlineData("nonstop", false)]   // Contains "stop" but as part of another word
    public void IsStopCommand_NoStopWord_ReturnsFalse(string text, bool expected)
    {
        // Act
        var result = ContinuousListenerWorker.IsStopCommand(text);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion
}
