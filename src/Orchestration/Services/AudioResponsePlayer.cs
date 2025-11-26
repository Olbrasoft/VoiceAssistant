using System.Diagnostics;

namespace Olbrasoft.VoiceAssistant.Orchestration.Services;

/// <summary>
/// Service for playing audio response files (MP3) on Linux using external audio players.
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
    /// Plays an audio file synchronously using Linux audio players (mpg123, paplay, or ffplay).
    /// </summary>
    /// <param name="fileName">Name of the audio file (e.g., "ano.mp3").</param>
    public Task PlayAsync(string fileName)
    {
        _logger.LogInformation("🔊 Playing audio: {FileName}", fileName);
        
        var filePath = Path.Combine(_audioDirectory, fileName);
        
        if (!File.Exists(filePath))
        {
            _logger.LogError("Audio file not found: {FilePath}", filePath);
            return Task.CompletedTask;
        }
        
        try
        {
            // Use external AudioPlayer project with detailed logging (call dotnet directly, not via bash)
            var audioPlayerDll = "/home/jirka/voice-assistant/audioplayer/AudioPlayer.dll";
            
            var audioPlayers = File.Exists(audioPlayerDll)
                ? new[] { ("/home/jirka/.dotnet/dotnet", audioPlayerDll) } // Direct dotnet call
                : new[] { ("ffplay", "-nodisp -autoexit -loglevel quiet") }; // Fallback to ffplay
            
            foreach (var (player, args) in audioPlayers)
            {
                _logger.LogInformation("🔄 Trying audio player: {Player}", player);
                
                if (TryPlayWithPlayer(player, args, filePath))
                {
                    _logger.LogInformation("✅ Audio played successfully with {Player}", player);
                    return Task.CompletedTask;
                }
                else
                {
                    _logger.LogWarning("⚠️  Audio player {Player} failed, trying next...", player);
                }
            }
            
            _logger.LogError("No suitable audio player found. Install mpg123, paplay, or ffplay.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error playing audio file: {FilePath}", filePath);
        }
        
        return Task.CompletedTask;
    }
    
    private bool TryPlayWithPlayer(string playerCommand, string args, string filePath)
    {
        try
        {
            var processId = Guid.NewGuid().ToString("N").Substring(0, 6);
            _logger.LogInformation("🎵 [Process {ProcessId}] Starting {Player} with args: {Args}", processId, playerCommand, args);
            
            var startInfo = new ProcessStartInfo
            {
                FileName = playerCommand,
                Arguments = $"{args} \"{filePath}\"".Trim(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            _logger.LogInformation("🎵 [Process {ProcessId}] Process.Start() called", processId);
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogWarning("🎵 [Process {ProcessId}] Process returned null", processId);
                return false;
            }
            
            _logger.LogInformation("🎵 [Process {ProcessId}] PID={Pid}, waiting for exit...", processId, process.Id);
            process.WaitForExit(); // SYNCHRONOUS - wait for process to finish
            _logger.LogInformation("🎵 [Process {ProcessId}] Exited with code {ExitCode}", processId, process.ExitCode);
            return process.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Player not found, try next one
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to play with {Player}", playerCommand);
            return false;
        }
    }
}
