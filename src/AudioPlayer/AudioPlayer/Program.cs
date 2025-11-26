using System.Diagnostics;

namespace AudioPlayer;

class Program
{
    private static readonly string LogFile = "/tmp/audioplayer.log";
    
    static int Main(string[] args)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var pid = Environment.ProcessId;
        var invocationId = Guid.NewGuid().ToString("N").Substring(0, 8);
        
        Log($"========================================");
        Log($"[{invocationId}] STARTED");
        Log($"[{invocationId}] PID: {pid}");
        Log($"[{invocationId}] Timestamp: {timestamp}");
        Log($"[{invocationId}] Args count: {args.Length}");
        
        if (args.Length == 0)
        {
            Log($"[{invocationId}] ERROR: No audio file specified");
            Console.Error.WriteLine("Usage: AudioPlayer <audio-file.mp3>");
            return 1;
        }
        
        var audioFile = args[0];
        Log($"[{invocationId}] Audio file: {audioFile}");
        
        if (!File.Exists(audioFile))
        {
            Log($"[{invocationId}] ERROR: File not found: {audioFile}");
            Console.Error.WriteLine($"Error: File not found: {audioFile}");
            return 1;
        }
        
        try
        {
            // Convert MP3 to WAV in memory using ffmpeg, then play with aplay
            Log($"[{invocationId}] Converting MP3 to WAV with ffmpeg...");
            
            var tempWav = $"/tmp/audio-{invocationId}.wav";
            
            // Step 1: Convert MP3 to WAV
            var ffmpegStartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{audioFile}\" -acodec pcm_s16le -ar 44100 \"{tempWav}\" -y",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using (var ffmpegProcess = Process.Start(ffmpegStartInfo))
            {
                if (ffmpegProcess == null)
                {
                    Log($"[{invocationId}] ERROR: Failed to start ffmpeg");
                    return 1;
                }
                
                Log($"[{invocationId}] ffmpeg PID: {ffmpegProcess.Id}");
                ffmpegProcess.WaitForExit();
                
                if (ffmpegProcess.ExitCode != 0)
                {
                    Log($"[{invocationId}] ERROR: ffmpeg failed with code {ffmpegProcess.ExitCode}");
                    return 1;
                }
                
                Log($"[{invocationId}] ffmpeg completed successfully");
            }
            
            // Step 2: Play WAV with aplay (direct ALSA)
            Log($"[{invocationId}] Starting aplay...");
            
            var startInfo = new ProcessStartInfo
            {
                FileName = "aplay",
                Arguments = $"-q \"{tempWav}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            var stopwatch = Stopwatch.StartNew();
            
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                Log($"[{invocationId}] ERROR: Failed to start aplay");
                File.Delete(tempWav);
                return 1;
            }
            
            Log($"[{invocationId}] aplay PID: {process.Id}");
            
            process.WaitForExit();
            stopwatch.Stop();
            
            // Clean up temp file
            try { File.Delete(tempWav); } catch { }
            
            Log($"[{invocationId}] aplay exited with code: {process.ExitCode}");
            Log($"[{invocationId}] Duration: {stopwatch.ElapsedMilliseconds}ms");
            Log($"[{invocationId}] COMPLETED");
            Log($"========================================");
            
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            Log($"[{invocationId}] EXCEPTION: {ex.Message}");
            Log($"[{invocationId}] Stack trace: {ex.StackTrace}");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
    
    private static void Log(string message)
    {
        try
        {
            File.AppendAllText(LogFile, message + Environment.NewLine);
        }
        catch
        {
            // Ignore logging errors
        }
    }
}
