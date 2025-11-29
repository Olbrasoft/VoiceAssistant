using System.ComponentModel;

namespace VoiceAssistant.Shared.Data.Enums;

/// <summary>
/// Source of the speech lock - who created the lock.
/// This enum is the single source of truth for lock sources.
/// Values are seeded to SpeechLockSources lookup table.
/// </summary>
public enum SpeechLockSource
{
    /// <summary>
    /// ContinuousListener - locked when wake word detected and collecting command.
    /// </summary>
    [Description("ContinuousListener - wake word command collection")]
    ContinuousListener = 1,

    /// <summary>
    /// Push-to-talk - locked when user is recording via CapsLock.
    /// </summary>
    [Description("Push-to-talk - CapsLock recording")]
    PushToTalk = 2,

    /// <summary>
    /// Manual lock via API or other means.
    /// </summary>
    [Description("Manual lock via API")]
    Manual = 3
}
