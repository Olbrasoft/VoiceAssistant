namespace Olbrasoft.VoiceAssistant.ContinuousListener;

/// <summary>
/// Configuration options for ContinuousListener.
/// </summary>
public class ContinuousListenerOptions
{
    public const string SectionName = "ContinuousListener";

    /// <summary>
    /// Audio sample rate in Hz. Default: 16000.
    /// </summary>
    public int SampleRate { get; set; } = 16000;

    /// <summary>
    /// VAD chunk size in milliseconds. Default: 32ms.
    /// </summary>
    public int VadChunkMs { get; set; } = 32;

    /// <summary>
    /// Pre-buffer duration in milliseconds. Default: 1000ms.
    /// </summary>
    public int PreBufferMs { get; set; } = 1000;

    /// <summary>
    /// Post-silence duration to end recording in milliseconds. Default: 600ms.
    /// </summary>
    public int PostSilenceMs { get; set; } = 600;

    /// <summary>
    /// Minimum recording duration in milliseconds. Default: 500ms.
    /// </summary>
    public int MinRecordingMs { get; set; } = 500;

    /// <summary>
    /// RMS threshold for speech detection. Default: 0.08.
    /// </summary>
    public float SilenceThreshold { get; set; } = 0.08f;

    /// <summary>
    /// Wake words to detect (case-insensitive).
    /// </summary>
    public string[] WakeWords { get; set; } = ["opencode", "open code", "počítači"];

    /// <summary>
    /// Path to Whisper model file (accurate, used for full command transcription).
    /// </summary>
    public string WhisperModelPath { get; set; } = "/home/jirka/voice-assistant/push-to-talk-dictation/models/ggml-medium.bin";

    /// <summary>
    /// Path to fast Whisper model file (used for quick wake word detection).
    /// </summary>
    public string WhisperFastModelPath { get; set; } = "/home/jirka/voice-assistant/push-to-talk-dictation/models/ggml-base.bin";

    /// <summary>
    /// Language for Whisper transcription.
    /// </summary>
    public string WhisperLanguage { get; set; } = "cs";

    /// <summary>
    /// Path to directory with "počítači" wake word audio responses.
    /// </summary>
    public string ComputerResponsesPath { get; set; } = "/home/jirka/voice-assistant/voice-output/computer-responses";

    /// <summary>
    /// Path to directory with "opencode" wake word audio responses.
    /// </summary>
    public string OpenCodeResponsesPath { get; set; } = "/home/jirka/voice-assistant/voice-output/opencode-responses";

    /// <summary>
    /// Wake words that should play an audio acknowledgment response.
    /// These are "system" wake words that respond with voice but don't dispatch to OpenCode.
    /// </summary>
    public string[] AudioResponseWakeWords { get; set; } = ["počítači"];

    /// <summary>
    /// Wake words that dispatch commands to OpenCode.
    /// </summary>
    public string[] OpenCodeWakeWords { get; set; } = ["opencode", "open code"];

    /// <summary>
    /// Post-silence duration after OpenCode wake word to collect full command (ms). Default: 2500ms.
    /// </summary>
    public int OpenCodePostSilenceMs { get; set; } = 2500;

    // Computed properties
    public int ChunkSizeBytes => SampleRate * VadChunkMs / 1000 * 2; // 16-bit = 2 bytes per sample
    public int PreBufferMaxBytes => SampleRate * PreBufferMs / 1000 * 2;
}
