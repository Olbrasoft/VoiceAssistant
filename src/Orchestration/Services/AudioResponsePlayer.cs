using NAudio.Wave;

namespace Olbrasoft.VoiceAssistant.Orchestration.Services;

/// <summary>
/// Service for playing audio response files (MP3).
/// </summary>
public class AudioResponsePlayer
{
    private readonly ILogger<AudioResponsePlayer> _logger;
    private readonly string _audioDirectory;
    
    public AudioResponsePlayer(ILogger<AudioResponsePlayer> logger, IConfiguration configuration)
    {
        _logger = logger;
        _audioDirectory = configuration["AudioDirectory"] ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio");
        
        if (!Directory.Exists(_audioDirectory))
        {
            _logger.LogWarning("Audio directory does not exist: {Directory}", _audioDirectory);
            Directory.CreateDirectory(_audioDirectory);
        }
    }
    
    /// <summary>
    /// Plays an audio file asynchronously.
    /// </summary>
    /// <param name="fileName">Name of the audio file (e.g., "ano.mp3").</param>
    public async Task PlayAsync(string fileName)
    {
        var filePath = Path.Combine(_audioDirectory, fileName);
        
        if (!File.Exists(filePath))
        {
            _logger.LogError("Audio file not found: {FilePath}", filePath);
            return;
        }
        
        try
        {
            _logger.LogInformation("Playing audio: {FileName}", fileName);
            
            using var audioFile = new AudioFileReader(filePath);
            using var outputDevice = new WaveOutEvent();
            
            outputDevice.Init(audioFile);
            outputDevice.Play();
            
            // Wait for playback to complete
            while (outputDevice.PlaybackState == PlaybackState.Playing)
            {
                await Task.Delay(100);
            }
            
            _logger.LogInformation("Audio playback completed: {FileName}", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error playing audio file: {FilePath}", filePath);
        }
    }
}
