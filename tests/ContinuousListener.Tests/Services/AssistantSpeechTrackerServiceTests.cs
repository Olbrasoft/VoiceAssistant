using Microsoft.Extensions.Logging;
using NSubstitute;
using Olbrasoft.Text.Similarity;
using Olbrasoft.VoiceAssistant.ContinuousListener.Services;
using Xunit;

namespace ContinuousListener.Tests.Services;

public class AssistantSpeechTrackerServiceTests
{
    private readonly ILogger<AssistantSpeechTrackerService> _logger;
    private readonly IStringSimilarity _stringSimilarity;
    private readonly AssistantSpeechTrackerService _sut;

    public AssistantSpeechTrackerServiceTests()
    {
        _logger = Substitute.For<ILogger<AssistantSpeechTrackerService>>();
        _stringSimilarity = new LevenshteinSimilarity();
        _sut = new AssistantSpeechTrackerService(_logger, _stringSimilarity);
    }

    #region Basic State Tests

    [Fact]
    public void GetHistoryCount_WhenNotStarted_ReturnsZero()
    {
        // Act
        var result = _sut.GetHistoryCount();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetHistoryCount_WhenSpeaking_ReturnsOne()
    {
        // Arrange
        _sut.StartSpeaking("Test text");

        // Act
        var result = _sut.GetHistoryCount();

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void GetHistoryCount_MultipleMessages_CountsAll()
    {
        // Arrange
        _sut.StartSpeaking("First message");
        _sut.StartSpeaking("Second message");
        _sut.StartSpeaking("Third message");

        // Act
        var result = _sut.GetHistoryCount();

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public void ClearHistory_RemovesAllMessages()
    {
        // Arrange
        _sut.StartSpeaking("First message");
        _sut.StartSpeaking("Second message");

        // Act
        _sut.ClearHistory();
        var result = _sut.GetHistoryCount();

        // Assert
        Assert.Equal(0, result);
    }

    #endregion

    #region FilterEchoFromTranscription Tests

    [Fact]
    public void FilterEchoFromTranscription_ExactMatch_ReturnsEmpty()
    {
        // Arrange
        const string text = "Úkol byl úspěšně dokončen";
        _sut.StartSpeaking(text);

        // Act
        var result = _sut.FilterEchoFromTranscription(text);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FilterEchoFromTranscription_SimilarTextWithoutDiacritics_ReturnsEmpty()
    {
        // Arrange - TTS says with diacritics
        _sut.StartSpeaking("Úkol byl úspěšně dokončen");

        // Act - Whisper transcribes without diacritics
        var result = _sut.FilterEchoFromTranscription("ukol byl uspesne dokoncen");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FilterEchoFromTranscription_SimilarTextWithPunctuation_ReturnsEmpty()
    {
        // Arrange
        _sut.StartSpeaking("Úkol byl úspěšně dokončen.");

        // Act - transcription without punctuation
        var result = _sut.FilterEchoFromTranscription("Úkol byl úspěšně dokončen");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FilterEchoFromTranscription_CompletelyDifferentText_ReturnsOriginal()
    {
        // Arrange
        _sut.StartSpeaking("Úkol byl úspěšně dokončen");

        // Act
        var result = _sut.FilterEchoFromTranscription("Jaké je dnes počasí");

        // Assert
        Assert.Equal("Jaké je dnes počasí", result);
    }

    [Fact]
    public void FilterEchoFromTranscription_WhenNoHistory_ReturnsOriginal()
    {
        // Act - no StartSpeaking called
        var result = _sut.FilterEchoFromTranscription("Some text");

        // Assert
        Assert.Equal("Some text", result);
    }

    [Fact]
    public void FilterEchoFromTranscription_CaseInsensitive_FiltersEcho()
    {
        // Arrange
        _sut.StartSpeaking("ÚKOL BYL ÚSPĚŠNĚ DOKONČEN");

        // Act
        var result = _sut.FilterEchoFromTranscription("úkol byl úspěšně dokončen");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FilterEchoFromTranscription_EchoPlusUserSpeech_ExtractsUserSpeech()
    {
        // Arrange - Assistant says "Rozumím"
        _sut.StartSpeaking("Rozumím");

        // Act - Whisper captures "Rozumím co je nového" (echo + user's question)
        var result = _sut.FilterEchoFromTranscription("Rozumím co je nového");

        // Assert
        Assert.Equal("co je nového", result);
    }

    [Fact]
    public void FilterEchoFromTranscription_EchoWithDiacriticsError_StillFilters()
    {
        // Arrange - TTS with proper Czech
        _sut.StartSpeaking("Připravil jsem odpověď");

        // Act - Whisper without diacritics + user text
        var result = _sut.FilterEchoFromTranscription("Pripravil jsem odpoved stop");

        // Assert
        Assert.Equal("stop", result);
    }

    [Fact]
    public void FilterEchoFromTranscription_EmptyTranscription_ReturnsEmpty()
    {
        // Arrange
        _sut.StartSpeaking("Test");

        // Act
        var result = _sut.FilterEchoFromTranscription("");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FilterEchoFromTranscription_NullTranscription_ReturnsNull()
    {
        // Arrange
        _sut.StartSpeaking("Test");

        // Act
        var result = _sut.FilterEchoFromTranscription(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FilterEchoFromTranscription_WhitespaceOnlyTranscription_ReturnsOriginal()
    {
        // Arrange
        _sut.StartSpeaking("Test");

        // Act
        var result = _sut.FilterEchoFromTranscription("   ");

        // Assert
        Assert.Equal("   ", result);
    }

    [Fact]
    public void FilterEchoFromTranscription_LongEchoPlusShortUserSpeech_Works()
    {
        // Arrange - longer TTS text
        _sut.StartSpeaking("Našel jsem tři soubory a upravil jsem je");

        // Act - Whisper captures the echo plus user interrupt "stop"
        var result = _sut.FilterEchoFromTranscription(
            "Našel jsem tři soubory a upravil jsem je stop prosím");

        // Assert
        Assert.Equal("stop prosím", result);
    }

    [Fact]
    public void FilterEchoFromTranscription_MultipleEchoes_FiltersAll()
    {
        // Arrange - Multiple TTS messages
        _sut.StartSpeaking("První zpráva");
        _sut.StartSpeaking("Druhá zpráva");

        // Act - Whisper captures both echoes + user text
        var result = _sut.FilterEchoFromTranscription("První zpráva Druhá zpráva a teď já");

        // Assert - should filter both echoes
        Assert.Equal("a teď já", result);
    }

    #endregion

    #region Czech Language Specific Tests

    [Theory]
    [InlineData("Úspěšně dokončeno", "uspesne dokonceno")]
    [InlineData("Příliš žluťoučký kůň", "prilis zlutoucky kun")]
    [InlineData("Čeština je krásná", "cestina je krasna")]
    [InlineData("Řeřicha", "rericha")]
    public void FilterEchoFromTranscription_CzechDiacriticsVariations_FiltersEcho(string ttsText, string whisperText)
    {
        // Arrange
        _sut.StartSpeaking(ttsText);

        // Act
        var result = _sut.FilterEchoFromTranscription(whisperText);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FilterEchoFromTranscription_CzechQuotes_Normalized()
    {
        // Arrange - with Czech quotes „ " (using escape sequences)
        _sut.StartSpeaking("Řekl jsem \u201Eahoj\u201C a odešel");

        // Act - without quotes
        var result = _sut.FilterEchoFromTranscription("Řekl jsem ahoj a odešel");

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region Legacy API Tests (Obsolete methods)

    [Fact]
    public void IsAssistantSpeech_ExactMatch_ReturnsTrue()
    {
        // Arrange
        const string text = "Úkol byl úspěšně dokončen";
        _sut.StartSpeaking(text);

        // Act
#pragma warning disable CS0618 // Type or member is obsolete
        var result = _sut.IsAssistantSpeech(text);
#pragma warning restore CS0618

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAssistantSpeech_CompletelyDifferentText_ReturnsFalse()
    {
        // Arrange
        _sut.StartSpeaking("Úkol byl úspěšně dokončen");

        // Act
#pragma warning disable CS0618 // Type or member is obsolete
        var result = _sut.IsAssistantSpeech("Jaké je dnes počasí");
#pragma warning restore CS0618

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetCurrentSpeechText_WhenSpeaking_ReturnsLastText()
    {
        // Arrange
        _sut.StartSpeaking("First text");
        _sut.StartSpeaking("Second text");

        // Act
#pragma warning disable CS0618 // Type or member is obsolete
        var result = _sut.GetCurrentSpeechText();
#pragma warning restore CS0618

        // Assert
        Assert.Equal("Second text", result);
    }

    [Fact]
    public void GetCurrentSpeechText_WhenNoHistory_ReturnsNull()
    {
        // Act
#pragma warning disable CS0618 // Type or member is obsolete
        var result = _sut.GetCurrentSpeechText();
#pragma warning restore CS0618

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void DetectEchoAndExtractRemaining_ExactEchoOnly_ReturnsEmptyRemaining()
    {
        // Arrange
        _sut.StartSpeaking("Úkol dokončen");

        // Act
#pragma warning disable CS0618 // Type or member is obsolete
        var (isEcho, similarity, remaining) = _sut.DetectEchoAndExtractRemaining("Úkol dokončen");
#pragma warning restore CS0618

        // Assert
        Assert.True(isEcho);
        Assert.Equal(1.0, similarity);
        Assert.Empty(remaining);
    }

    [Fact]
    public void DetectEchoAndExtractRemaining_NoEcho_ReturnsOriginalText()
    {
        // Arrange
        _sut.StartSpeaking("Úkol dokončen");

        // Act
#pragma warning disable CS0618 // Type or member is obsolete
        var (isEcho, similarity, remaining) = _sut.DetectEchoAndExtractRemaining("Jaké je počasí");
#pragma warning restore CS0618

        // Assert
        Assert.False(isEcho);
        Assert.Equal(0.0, similarity);
        Assert.Equal("Jaké je počasí", remaining);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void StartSpeaking_WithEmptyString_DoesNotAddToHistory()
    {
        // Arrange & Act
        _sut.StartSpeaking("");
        _sut.StartSpeaking("   ");
        _sut.StartSpeaking(null!);

        // Assert
        Assert.Equal(0, _sut.GetHistoryCount());
    }

    [Fact]
    public void StartSpeaking_HistoryLimitedToMaxSize()
    {
        // Arrange - Add more than MaxHistorySize (10) messages
        for (int i = 0; i < 15; i++)
        {
            _sut.StartSpeaking($"Message {i}");
        }

        // Act
        var count = _sut.GetHistoryCount();

        // Assert - should be limited to 10
        Assert.Equal(10, count);
    }

    #endregion

    #region ContainsStopWord Tests

    [Fact]
    public void ContainsStopWord_WhenHistoryEmpty_ReturnsFalse()
    {
        // Arrange
        var stopWords = new[] { "stop", "stůj", "ticho" };

        // Act
        var result = _sut.ContainsStopWord(stopWords);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsStopWord_WhenHistoryContainsStopWord_ReturnsTrue()
    {
        // Arrange
        _sut.StartSpeaking("Počkej, musím to zastavit, stop!");
        var stopWords = new[] { "stop", "stůj", "ticho" };

        // Act
        var result = _sut.ContainsStopWord(stopWords);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsStopWord_WhenHistoryDoesNotContainStopWord_ReturnsFalse()
    {
        // Arrange
        _sut.StartSpeaking("Úkol byl úspěšně dokončen");
        var stopWords = new[] { "stop", "stůj", "ticho" };

        // Act
        var result = _sut.ContainsStopWord(stopWords);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsStopWord_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        _sut.StartSpeaking("STOP! To stačí.");
        var stopWords = new[] { "stop", "stůj", "ticho" };

        // Act
        var result = _sut.ContainsStopWord(stopWords);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsStopWord_WithCzechDiacritics_ReturnsTrue()
    {
        // Arrange
        _sut.StartSpeaking("Stůj, nepokračuj dál");
        var stopWords = new[] { "stop", "stůj", "ticho" };

        // Act
        var result = _sut.ContainsStopWord(stopWords);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsStopWord_InMultipleMessages_ReturnsTrue()
    {
        // Arrange
        _sut.StartSpeaking("První zpráva");
        _sut.StartSpeaking("Druhá zpráva se slovem ticho");
        _sut.StartSpeaking("Třetí zpráva");
        var stopWords = new[] { "stop", "stůj", "ticho" };

        // Act
        var result = _sut.ContainsStopWord(stopWords);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsStopWord_WithPunctuation_ReturnsTrue()
    {
        // Arrange
        _sut.StartSpeaking("Dost! Ukončuji operaci.");
        var stopWords = new[] { "stop", "stůj", "ticho", "dost" };

        // Act
        var result = _sut.ContainsStopWord(stopWords);

        // Assert
        Assert.True(result);
    }

    #endregion
}
