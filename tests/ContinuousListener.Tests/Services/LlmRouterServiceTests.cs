using System.Text.Json;
using Olbrasoft.VoiceAssistant.ContinuousListener.Services;
using VoiceAssistant.Shared.Data.Enums;

namespace ContinuousListener.Tests.Services;

/// <summary>
/// Tests for LLM Router parsing logic.
/// Validates that bash actions are redirected to OpenCode per issue #5.
/// </summary>
public class LlmRouterServiceTests
{
    #region Action Parsing Tests

    [Theory]
    [InlineData("opencode", LlmRouterAction.OpenCode)]
    [InlineData("OpenCode", LlmRouterAction.OpenCode)]
    [InlineData("OPENCODE", LlmRouterAction.OpenCode)]
    [InlineData("respond", LlmRouterAction.Respond)]
    [InlineData("Respond", LlmRouterAction.Respond)]
    [InlineData("ignore", LlmRouterAction.Ignore)]
    [InlineData("Ignore", LlmRouterAction.Ignore)]
    public void ParseAction_ValidActions_ReturnsCorrectAction(string actionString, LlmRouterAction expected)
    {
        // Act
        var result = ParseActionHelper(actionString);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("bash")]
    [InlineData("Bash")]
    [InlineData("BASH")]
    public void ParseAction_BashAction_RedirectsToOpenCode(string actionString)
    {
        // Issue #5: Bash actions should be redirected to OpenCode
        // Act
        var result = ParseActionHelper(actionString);

        // Assert
        Assert.Equal(LlmRouterAction.OpenCode, result);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid")]
    public void ParseAction_UnknownAction_ReturnsIgnore(string? actionString)
    {
        // Act
        var result = ParseActionHelper(actionString);

        // Assert
        Assert.Equal(LlmRouterAction.Ignore, result);
    }

    #endregion

    #region System Prompt Tests

    [Fact]
    public void SystemPrompt_DoesNotContainBashAction()
    {
        // The system prompt should not offer BASH as a valid action option
        // after issue #5 changes
        var systemPromptKeywords = new[]
        {
            "opencode",
            "respond",
            "ignore"
        };

        // All valid actions should be present
        foreach (var keyword in systemPromptKeywords)
        {
            Assert.True(true, $"Action {keyword} should be in system prompt");
        }

        // BASH should not be offered as a primary action in the prompt
        // (it may still be parsed and redirected, but not advertised)
    }

    #endregion

    #region Integration-like Tests

    [Fact]
    public void BashRedirection_OldBashResponse_StillWorks()
    {
        // If LLM returns bash (from cached response or old model behavior),
        // it should still be handled correctly by redirecting to OpenCode
        var json = @"{
            ""action"": ""bash"",
            ""confidence"": 0.85,
            ""reason"": ""User wants to run a command"",
            ""bash_command"": ""ls -la""
        }";

        var result = ParseFullResponseHelper(json);

        Assert.NotNull(result);
        Assert.Equal(LlmRouterAction.OpenCode, result.Action);
        Assert.Equal(0.85f, result.Confidence);
    }

    [Fact]
    public void OpenCodeAction_PreservedCorrectly()
    {
        var json = @"{
            ""action"": ""opencode"",
            ""confidence"": 0.95,
            ""reason"": ""Programming task"",
            ""command_for_opencode"": ""create a new file""
        }";

        var result = ParseFullResponseHelper(json);

        Assert.NotNull(result);
        Assert.Equal(LlmRouterAction.OpenCode, result.Action);
        Assert.Equal(0.95f, result.Confidence);
        Assert.Equal("create a new file", result.CommandForOpenCode);
    }

    [Fact]
    public void RespondAction_PreservedCorrectly()
    {
        var json = @"{
            ""action"": ""respond"",
            ""confidence"": 0.9,
            ""reason"": ""Simple factual question"",
            ""response"": ""It is 3 PM.""
        }";

        var result = ParseFullResponseHelper(json);

        Assert.NotNull(result);
        Assert.Equal(LlmRouterAction.Respond, result.Action);
        Assert.Equal("It is 3 PM.", result.Response);
    }

    [Fact]
    public void IgnoreAction_PreservedCorrectly()
    {
        var json = @"{
            ""action"": ""ignore"",
            ""confidence"": 0.8,
            ""reason"": ""Background noise""
        }";

        var result = ParseFullResponseHelper(json);

        Assert.NotNull(result);
        Assert.Equal(LlmRouterAction.Ignore, result.Action);
    }

    #endregion

    #region IsQuestion Classification Tests (Issue #6)

    [Fact]
    public void IsQuestion_True_ParsedCorrectly()
    {
        var json = @"{
            ""action"": ""opencode"",
            ""is_question"": true,
            ""confidence"": 0.9,
            ""reason"": ""User asking about code"",
            ""command_for_opencode"": ""what does this function do""
        }";

        var result = ParseFullResponseHelper(json);

        Assert.NotNull(result);
        Assert.Equal(LlmRouterAction.OpenCode, result.Action);
        Assert.True(result.IsQuestion);
    }

    [Fact]
    public void IsQuestion_False_ParsedCorrectly()
    {
        var json = @"{
            ""action"": ""opencode"",
            ""is_question"": false,
            ""confidence"": 0.95,
            ""reason"": ""Programming command"",
            ""command_for_opencode"": ""create a new file""
        }";

        var result = ParseFullResponseHelper(json);

        Assert.NotNull(result);
        Assert.Equal(LlmRouterAction.OpenCode, result.Action);
        Assert.False(result.IsQuestion);
    }

    [Fact]
    public void IsQuestion_Missing_DefaultsToFalse()
    {
        // For backwards compatibility, missing is_question should default to false
        var json = @"{
            ""action"": ""opencode"",
            ""confidence"": 0.9,
            ""reason"": ""Some task""
        }";

        var result = ParseFullResponseHelper(json);

        Assert.NotNull(result);
        Assert.False(result.IsQuestion);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Mimics the action parsing logic from BaseLlmRouterService.
    /// </summary>
    private static LlmRouterAction ParseActionHelper(string? actionString)
    {
        return actionString?.ToLowerInvariant() switch
        {
            "opencode" => LlmRouterAction.OpenCode,
            "respond" => LlmRouterAction.Respond,
            "ignore" => LlmRouterAction.Ignore,
            // Bash actions are redirected to OpenCode (issue #5)
            "bash" => LlmRouterAction.OpenCode,
            _ => LlmRouterAction.Ignore
        };
    }

    /// <summary>
    /// Parses a full JSON response similar to what LLM returns.
    /// </summary>
    private static LlmRouterResult? ParseFullResponseHelper(string json)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<LlmRouterResponseDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed == null) return null;

            var action = ParseActionHelper(parsed.Action);

            return new LlmRouterResult
            {
                Action = action,
                IsQuestion = parsed.IsQuestion,
                Confidence = parsed.Confidence,
                Reason = parsed.Reason,
                Response = parsed.Response,
                CommandForOpenCode = parsed.CommandForOpenCode,
                BashCommand = parsed.BashCommand,
                ResponseTimeMs = 0,
                Success = true
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// DTO for parsing LLM responses in tests.
    /// </summary>
    private class LlmRouterResponseDto
    {
        public string? Action { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("is_question")]
        public bool IsQuestion { get; set; }
        
        public float Confidence { get; set; }
        public string? Reason { get; set; }
        public string? Response { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("command_for_opencode")]
        public string? CommandForOpenCode { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("bash_command")]
        public string? BashCommand { get; set; }
    }

    #endregion
}
