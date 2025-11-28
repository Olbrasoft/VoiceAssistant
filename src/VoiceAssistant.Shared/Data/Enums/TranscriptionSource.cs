using System.ComponentModel;

namespace VoiceAssistant.Shared.Data.Enums;

/// <summary>
/// Source of the transcription - where the audio came from.
/// This enum is the single source of truth for transcription sources.
/// Values are seeded to TranscriptionSources lookup table.
/// </summary>
public enum TranscriptionSource
{
    /// <summary>
    /// Push-to-talk dictation (CapsLock trigger).
    /// </summary>
    [Description("Push-to-talk dictation (CapsLock trigger)")]
    PushToTalk = 1,

    /// <summary>
    /// Continuous listening mode (always-on).
    /// </summary>
    [Description("Continuous listening mode (always-on)")]
    ContinuousListener = 2,

    /// <summary>
    /// Wake word triggered (e.g., "Jarvis").
    /// </summary>
    [Description("Wake word triggered (e.g., Jarvis)")]
    WakeWord = 3,

    /// <summary>
    /// Manual file upload or API call.
    /// </summary>
    [Description("Manual file upload or API call")]
    Manual = 4
}
