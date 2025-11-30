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
    /// Post-silence duration to end recording in milliseconds. Default: 1500ms.
    /// With Groq Router, we wait longer to capture complete utterances.
    /// </summary>
    public int PostSilenceMs { get; set; } = 1500;

    /// <summary>
    /// Minimum recording duration in milliseconds. Default: 800ms.
    /// </summary>
    public int MinRecordingMs { get; set; } = 800;

    /// <summary>
    /// Path to Silero VAD ONNX model file.
    /// </summary>
    public string SileroVadModelPath { get; set; } = "/home/jirka/voice-assistant/models/silero_vad.onnx";

    /// <summary>
    /// Path to Whisper model file for transcription.
    /// </summary>
    public string WhisperModelPath { get; set; } = "/home/jirka/voice-assistant/automatic-speech-recognition-models/ggml-medium.bin";

    /// <summary>
    /// Language for Whisper transcription.
    /// </summary>
    public string WhisperLanguage { get; set; } = "cs";

    /// <summary>
    /// Maximum audio segment duration for transcription in milliseconds. Default: 60000ms (60 seconds).
    /// Longer audio will be truncated to prevent Whisper.net issues.
    /// </summary>
    public int MaxSegmentMs { get; set; } = 60000;

    // Computed properties
    public int ChunkSizeBytes => SampleRate * VadChunkMs / 1000 * 2; // 16-bit = 2 bytes per sample
    public int PreBufferMaxBytes => SampleRate * PreBufferMs / 1000 * 2;
    public int MaxSegmentBytes => SampleRate * MaxSegmentMs / 1000 * 2; // Max audio size for transcription
}
