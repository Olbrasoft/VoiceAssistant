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
    public string[] WakeWords { get; set; } = ["jarvis", "opencode", "open code", "počítači"];

    /// <summary>
    /// Path to Whisper model file.
    /// </summary>
    public string WhisperModelPath { get; set; } = "/home/jirka/voice-assistant/push-to-talk-dictation/models/ggml-medium.bin";

    /// <summary>
    /// Language for Whisper transcription.
    /// </summary>
    public string WhisperLanguage { get; set; } = "cs";

    // Computed properties
    public int ChunkSizeBytes => SampleRate * VadChunkMs / 1000 * 2; // 16-bit = 2 bytes per sample
    public int PreBufferMaxBytes => SampleRate * PreBufferMs / 1000 * 2;
}
