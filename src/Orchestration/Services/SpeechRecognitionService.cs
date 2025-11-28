using System.Diagnostics;
using System.Text.Json;

namespace Olbrasoft.VoiceAssistant.Orchestration.Services;

/// <summary>
/// Service for recording audio and transcribing speech to text.
/// Uses Python scripts for recording and transcription (Linux compatible).
/// </summary>
public class SpeechRecognitionService
{
    private readonly ILogger<SpeechRecognitionService> _logger;
    private readonly string _recordingScriptPath;
    private readonly string _transcriptionScriptPath;
    private readonly string _tempDirectory;

    public SpeechRecognitionService(ILogger<SpeechRecognitionService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _recordingScriptPath = configuration["RecordingScriptPath"] 
            ?? "/home/jirka/Olbrasoft/VoiceAssistant/scripts/record-audio.py";
        _transcriptionScriptPath = configuration["TranscriptionScriptPath"] 
            ?? "/home/jirka/Olbrasoft/VoiceAssistant/scripts/transcribe-audio.py";
        _tempDirectory = Path.Combine(Path.GetTempPath(), "voice-assistant");
        Directory.CreateDirectory(_tempDirectory);
    }

    /// <summary>
    /// Records audio from microphone with automatic silence detection using Python script.
    /// </summary>
    /// <param name="maxDurationSeconds">Maximum recording duration in seconds.</param>
    /// <param name="silenceThresholdAmplitude">Amplitude threshold for silence detection (0-32767).</param>
    /// <param name="maxSilenceSeconds">Maximum seconds of silence before stopping.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Path to recorded WAV file.</returns>
    public async Task<string> RecordAudioAsync(
        int maxDurationSeconds = 30,
        int silenceThresholdAmplitude = 800,
        double maxSilenceSeconds = 3.0,
        CancellationToken cancellationToken = default)
    {
        var outputFile = Path.Combine(_tempDirectory, $"recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav");
        
        _logger.LogInformation("🎤 Recording audio to {File}...", outputFile);

        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "/home/jirka/miniconda3/bin/python3",
                Arguments = $"\"{_recordingScriptPath}\" \"{outputFile}\" {maxDurationSeconds} {silenceThresholdAmplitude} {maxSilenceSeconds}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
            {
                _logger.LogError("❌ Failed to start recording process");
                return string.Empty;
            }

            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var error = await errorTask;

            // Log stderr (contains progress messages)
            if (!string.IsNullOrWhiteSpace(error))
            {
                foreach (var line in error.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    _logger.LogInformation(line);
                }
            }

            if (process.ExitCode != 0)
            {
                _logger.LogError("❌ Recording failed with exit code {ExitCode}", process.ExitCode);
                return string.Empty;
            }

            if (!File.Exists(outputFile))
            {
                _logger.LogError("❌ Recording file not created: {File}", outputFile);
                // List directory contents for debugging
                try
                {
                    var files = Directory.GetFiles(_tempDirectory);
                    _logger.LogError("📁 Files in {Dir}: {Files}", _tempDirectory, string.Join(", ", files.Select(Path.GetFileName)));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to list directory");
                }
                return string.Empty;
            }

            _logger.LogInformation("✅ Recording file created: {File} ({Size} bytes)", outputFile, new FileInfo(outputFile).Length);
            return outputFile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Recording exception");
            return string.Empty;
        }
    }

    /// <summary>
    /// Transcribes audio file to text using faster-whisper Python script.
    /// </summary>
    /// <param name="audioFilePath">Path to WAV audio file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transcribed text, or empty string if transcription failed.</returns>
    public async Task<string> TranscribeAudioAsync(string audioFilePath, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if file exists before transcription
            if (!File.Exists(audioFilePath))
            {
                _logger.LogError("❌ Audio file does not exist before transcription: {File}", audioFilePath);
                // List directory contents
                try
                {
                    var dir = Path.GetDirectoryName(audioFilePath) ?? "/tmp/voice-assistant";
                    var files = Directory.GetFiles(dir);
                    _logger.LogError("📁 Files in {Dir}: {Files}", dir, string.Join(", ", files.Select(Path.GetFileName)));
                }
                catch { }
                return string.Empty;
            }

            _logger.LogInformation("🔄 Transcribing audio: {File} ({Size} bytes)", audioFilePath, new FileInfo(audioFilePath).Length);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "/home/jirka/miniconda3/bin/python3",
                Arguments = $"\"{_transcriptionScriptPath}\" \"{audioFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _logger.LogInformation("🐍 Running: {Python} {Args}", processStartInfo.FileName, processStartInfo.Arguments);

            using var process = Process.Start(processStartInfo);
            if (process == null)
            {
                _logger.LogError("❌ Failed to start transcription process");
                return string.Empty;
            }

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;

            // Debug: Log raw output
            _logger.LogInformation("📄 Transcription stdout: {Output}", output);
            if (!string.IsNullOrWhiteSpace(error))
            {
                _logger.LogInformation("📄 Transcription stderr: {Error}", error);
            }

            // Parse JSON output (works for both success and error cases)
            TranscriptionResult? result = null;
            try
            {
                var options = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                };
                result = JsonSerializer.Deserialize<TranscriptionResult>(output, options);
                
                _logger.LogInformation("🔍 Parsed result: Text={Text}, Error={Error}, Language={Language}", 
                    result?.Text ?? "(null)", result?.Error ?? "(null)", result?.Language ?? "(null)");
            }
            catch (JsonException)
            {
                _logger.LogError("❌ Transcription failed - invalid JSON: {Output}. stderr: {Error}", output, error);
                return string.Empty;
            }
            
            if (result?.Error != null)
            {
                _logger.LogError("❌ Transcription error: {Error}", result.Error);
                return string.Empty;
            }

            if (process.ExitCode != 0)
            {
                _logger.LogError("❌ Transcription failed with exit code {ExitCode}. stderr: {Error}", process.ExitCode, error);
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(result?.Text))
            {
                _logger.LogWarning("⚠️  Transcription empty or too short");
                return string.Empty;
            }

            _logger.LogInformation("📝 Transcribed: {Text}", result.Text);
            return result.Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Transcription exception");
            return string.Empty;
        }
    }

    /// <summary>
    /// Records audio and transcribes it to text in one operation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transcribed text, or empty string if failed.</returns>
    public async Task<string> RecordAndTranscribeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var audioFile = await RecordAudioAsync(cancellationToken: cancellationToken);
            
            if (string.IsNullOrEmpty(audioFile))
            {
                _logger.LogWarning("⚠️  Recording failed, skipping transcription");
                return string.Empty;
            }
            
            var text = await TranscribeAudioAsync(audioFile, cancellationToken);

            // Clean up temp file
            try
            {
                File.Delete(audioFile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temp audio file: {File}", audioFile);
            }

            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Record and transcribe failed");
            return string.Empty;
        }
    }

    /// <summary>
    /// Result from Python transcription script.
    /// </summary>
    private class TranscriptionResult
    {
        public string? Text { get; set; }
        public string? Error { get; set; }
        public string? Language { get; set; }
        public double? Duration { get; set; }
    }
}
