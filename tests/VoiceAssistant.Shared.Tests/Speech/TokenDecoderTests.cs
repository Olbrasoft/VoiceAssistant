using Olbrasoft.VoiceAssistant.Shared.Speech;

namespace Olbrasoft.VoiceAssistant.Shared.Tests.Speech;

public class TokenDecoderTests
{
    [Fact]
    public void Constructor_WithNonExistentFile_ShouldThrow()
    {
        // Act & Assert
        Assert.ThrowsAny<Exception>(() => new TokenDecoder("/non/existent/tokens.txt"));
    }

    [Fact]
    public void GetStartTokens_ForCzech_ShouldReturnCorrectSequence()
    {
        // Act
        var tokens = TokenDecoder.GetStartTokens("cs");

        // Assert
        Assert.Equal(4, tokens.Length);
        Assert.Equal(50258, tokens[0]); // StartOfTranscript
        Assert.Equal(50283, tokens[1]); // Czech language token
        Assert.Equal(50359, tokens[2]); // Transcribe
        Assert.Equal(50363, tokens[3]); // NoTimestamps
    }

    [Fact]
    public void GetStartTokens_ForEnglish_ShouldReturnCorrectSequence()
    {
        // Act
        var tokens = TokenDecoder.GetStartTokens("en");

        // Assert
        Assert.Equal(4, tokens.Length);
        Assert.Equal(50258, tokens[0]); // StartOfTranscript
        Assert.Equal(50259, tokens[1]); // English language token
        Assert.Equal(50359, tokens[2]); // Transcribe
        Assert.Equal(50363, tokens[3]); // NoTimestamps
    }

    [Fact]
    public void GetStartTokens_DefaultLanguage_ShouldBeCzech()
    {
        // Act
        var tokens = TokenDecoder.GetStartTokens();

        // Assert - default should be Czech (cs)
        Assert.Equal(50283, tokens[1]); // Czech language token
    }

    [Fact]
    public void GetStartTokens_ForUnknownLanguage_ShouldDefaultToEnglish()
    {
        // Act
        var tokens = TokenDecoder.GetStartTokens("unknown");

        // Assert
        Assert.Equal(50259, tokens[1]); // English language token (default fallback)
    }

    [Theory]
    [InlineData("en", 50259)]
    [InlineData("de", 50261)]
    [InlineData("fr", 50265)]
    [InlineData("cs", 50283)]
    [InlineData("pl", 50269)]
    [InlineData("ru", 50263)]
    public void GetStartTokens_ForVariousLanguages_ShouldReturnCorrectLanguageToken(
        string language, int expectedToken)
    {
        // Act
        var tokens = TokenDecoder.GetStartTokens(language);

        // Assert
        Assert.Equal(expectedToken, tokens[1]);
    }

    [Fact]
    public void GetStartTokens_ShouldAlwaysStartWithStartOfTranscript()
    {
        // Arrange
        var languages = new[] { "en", "cs", "de", "fr", "es", "ru" };

        // Act & Assert
        foreach (var lang in languages)
        {
            var tokens = TokenDecoder.GetStartTokens(lang);
            Assert.Equal(50258, tokens[0]); // StartOfTranscript
        }
    }

    [Fact]
    public void GetStartTokens_ShouldAlwaysEndWithNoTimestamps()
    {
        // Arrange
        var languages = new[] { "en", "cs", "de", "fr" };

        // Act & Assert
        foreach (var lang in languages)
        {
            var tokens = TokenDecoder.GetStartTokens(lang);
            Assert.Equal(50363, tokens[^1]); // NoTimestamps
        }
    }
}
