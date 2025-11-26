using System.Diagnostics;

namespace Olbrasoft.VoiceAssistant.WakeWordDetection;

/// <summary>
/// Linux ALSA-based audio capture using arecord command-line tool.
/// </summary>
public class AlsaAudioCapture : IAudioCapture
{
    private readonly int _sampleRate;
    private readonly int _channels;
    private readonly int _bitsPerSample;
    private readonly int _deviceNumber;
    private bool _isCapturing;
    private Process? _arecordProcess;
    private Task? _captureTask;
    private CancellationTokenSource? _cts;

    /// <inheritdoc/>
    public event EventHandler<AudioDataEventArgs>? AudioDataAvailable;

    /// <inheritdoc/>
    public int SampleRate => _sampleRate;

    /// <inheritdoc/>
    public int Channels => _channels;

    /// <inheritdoc/>
    public int BitsPerSample => _bitsPerSample;

    /// <inheritdoc/>
    public bool IsCapturing => _isCapturing;

    /// <summary>
    /// Initializes a new instance of the <see cref="AlsaAudioCapture"/> class.
    /// </summary>
    /// <param name="sampleRate">Sample rate in Hz (default: 16000).</param>
    /// <param name="channels">Number of channels (default: 1 for mono).</param>
    /// <param name="bitsPerSample">Bits per sample (default: 16).</param>
    /// <param name="deviceNumber">ALSA device number (default: 0).</param>
    public AlsaAudioCapture(
        int sampleRate = 16000,
        int channels = 1,
        int bitsPerSample = 16,
        int deviceNumber = 0)
    {
        _sampleRate = sampleRate;
        _channels = channels;
        _bitsPerSample = bitsPerSample;
        _deviceNumber = deviceNumber;
    }

    /// <inheritdoc/>
    public Task StartCaptureAsync(CancellationToken cancellationToken = default)
    {
        if (_isCapturing)
        {
            return Task.CompletedTask;
        }

        Console.WriteLine($"[AlsaAudioCapture] Starting PipeWire audio capture - Device: alsa_input.usb-FuZhou_Kingwayinfo_CO._LTD_TONOR_TC30_Audio_Device_20200707-00.mono-fallback, Rate: {_sampleRate} Hz, Channels: {_channels}");

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Use pw-record with PipeWire (resamples to 16kHz)
        _arecordProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "pw-record",
                Arguments = $"--target alsa_input.usb-FuZhou_Kingwayinfo_CO._LTD_TONOR_TC30_Audio_Device_20200707-00.mono-fallback --format s16 --rate {_sampleRate} --channels {_channels} -",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _arecordProcess.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine($"[AlsaAudioCapture] pw-record stderr: {e.Data}");
            }
        };

        _arecordProcess.Start();
        _arecordProcess.BeginErrorReadLine();

        _isCapturing = true;

        // Start reading audio data in background task
        _captureTask = Task.Run(async () => await CaptureAudioLoop(_cts.Token), _cts.Token);

        Console.WriteLine("[AlsaAudioCapture] PipeWire audio capture started");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Main audio capture loop - reads from arecord stdout.
    /// </summary>
    private async Task CaptureAudioLoop(CancellationToken cancellationToken)
    {
        try
        {
            const int frameSizeInSamples = 512;  // Vosk/Porcupine frame size
            const int bytesPerSample = 2;        // 16-bit = 2 bytes
            int frameSize = frameSizeInSamples * bytesPerSample;

            byte[] buffer = new byte[frameSize];
            var stream = _arecordProcess!.StandardOutput.BaseStream;

            Console.WriteLine($"[AlsaAudioCapture] Starting capture loop, frame size: {frameSize} bytes");
            int frameCount = 0;

            while (_isCapturing && !cancellationToken.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, frameSize, cancellationToken);

                if (bytesRead == 0)
                {
                    Console.WriteLine("[AlsaAudioCapture] End of audio stream reached");
                    break; // End of stream
                }

                frameCount++;
                if (frameCount % 100 == 0)
                {
                    Console.WriteLine($"[AlsaAudioCapture] Processed {frameCount} frames, last read: {bytesRead} bytes");
                }

                // Convert byte[] to short[] (16-bit PCM samples)
                short[] samples = new short[bytesRead / 2];
                Buffer.BlockCopy(buffer, 0, samples, 0, bytesRead);

                // Raise event with audio data
                AudioDataAvailable?.Invoke(this, new AudioDataEventArgs { AudioData = samples });
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[AlsaAudioCapture] Audio capture cancelled");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AlsaAudioCapture] Error in audio capture loop: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task StopCaptureAsync()
    {
        if (!_isCapturing)
        {
            return;
        }

        Console.WriteLine("[AlsaAudioCapture] Stopping PipeWire audio capture...");

        _isCapturing = false;
        _cts?.Cancel();

        // Kill pw-record process
        if (_arecordProcess != null && !_arecordProcess.HasExited)
        {
            _arecordProcess.Kill(entireProcessTree: true);
            await _arecordProcess.WaitForExitAsync();
        }

        // Wait for capture task to complete
        if (_captureTask != null)
        {
            await _captureTask;
        }

        Console.WriteLine("[AlsaAudioCapture] PipeWire audio capture stopped");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        StopCaptureAsync().GetAwaiter().GetResult();
        _cts?.Dispose();
        _arecordProcess?.Dispose();
    }
}
