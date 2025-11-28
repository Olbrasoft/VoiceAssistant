using Microsoft.Extensions.Configuration;
using Olbrasoft.VoiceAssistant.Shared.Input;
using Olbrasoft.VoiceAssistant.Shared.Speech;
using Olbrasoft.VoiceAssistant.Shared.TextInput;

namespace Olbrasoft.VoiceAssistant.PushToTalkDictation.Service;

/// <summary>
/// Background worker service for push-to-talk dictation.
/// Monitors keyboard for CapsLock state changes and controls audio recording.
/// Records when CapsLock is ON, stops and transcribes when CapsLock is OFF.
/// </summary>
public class DictationWorker : BackgroundService
{
    private readonly ILogger<DictationWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IKeyboardMonitor _keyboardMonitor;
    private readonly IAudioRecorder _audioRecorder;
    private readonly ISpeechTranscriber _speechTranscriber;
    private readonly ITextTyper _textTyper;
    private bool _isRecording;
    private DateTime? _recordingStartTime;
    private KeyCode _triggerKey;

    public DictationWorker(
        ILogger<DictationWorker> logger,
        IConfiguration configuration,
        IKeyboardMonitor keyboardMonitor,
        IAudioRecorder audioRecorder,
        ISpeechTranscriber speechTranscriber,
        ITextTyper textTyper)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _keyboardMonitor = keyboardMonitor ?? throw new ArgumentNullException(nameof(keyboardMonitor));
        _audioRecorder = audioRecorder ?? throw new ArgumentNullException(nameof(audioRecorder));
        _speechTranscriber = speechTranscriber ?? throw new ArgumentNullException(nameof(speechTranscriber));
        _textTyper = textTyper ?? throw new ArgumentNullException(nameof(textTyper));

        // Load configuration
        var triggerKeyName = _configuration.GetValue<string>("PushToTalkDictation:TriggerKey", "CapsLock");
        _triggerKey = Enum.Parse<KeyCode>(triggerKeyName);

        _logger.LogInformation("Dictation worker initialized. Trigger key: {TriggerKey}", _triggerKey);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Push-to-Talk Dictation Service starting...");

        try
        {
            // Subscribe to keyboard events
            _keyboardMonitor.KeyPressed += OnKeyPressed;
            _keyboardMonitor.KeyReleased += OnKeyReleased;

            _logger.LogInformation("Press {TriggerKey} to start dictation, release to stop", _triggerKey);

            // Start keyboard monitoring (doesn't block)
            await _keyboardMonitor.StartMonitoringAsync(stoppingToken);

            // Wait indefinitely until cancellation is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Dictation service is stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in dictation worker");
            throw;
        }
        finally
        {
            _keyboardMonitor.KeyPressed -= OnKeyPressed;
            _keyboardMonitor.KeyReleased -= OnKeyReleased;

            if (_isRecording)
            {
                await StopRecordingAsync();
            }
        }
    }

    private void OnKeyPressed(object? sender, KeyEventArgs e)
    {
        // We only care about the trigger key
        // Actual action is taken in OnKeyReleased after LED state updates
    }

    private void OnKeyReleased(object? sender, KeyEventArgs e)
    {
        if (e.Key != _triggerKey)
            return;

        // Read actual CapsLock state from LED - this is the reliable source of truth
        // LED state is updated by the kernel AFTER the key event is processed
        // Small delay to ensure LED state is updated
        Thread.Sleep(50);
        
        var capsLockOn = _keyboardMonitor.IsCapsLockOn();
        _logger.LogDebug("CapsLock released - LED state: {CapsLockOn}, Recording state: {Recording}", capsLockOn, _isRecording);

        if (capsLockOn && !_isRecording)
        {
            // CapsLock is ON and not recording - start recording
            _logger.LogInformation("CapsLock ON - starting dictation");
            _ = Task.Run(async () => await StartRecordingAsync());
        }
        else if (!capsLockOn && _isRecording)
        {
            // CapsLock is OFF and recording - stop and transcribe
            _logger.LogInformation("CapsLock OFF - stopping dictation");
            _ = Task.Run(async () => await StopRecordingAsync());
        }
        else
        {
            _logger.LogDebug("CapsLock state ({CapsLockOn}) matches recording state ({Recording}) - no action needed", 
                capsLockOn, _isRecording);
        }
    }

    private async Task StartRecordingAsync()
    {
        if (_isRecording)
        {
            _logger.LogWarning("Recording is already in progress");
            return;
        }

        try
        {
            _isRecording = true;
            _recordingStartTime = DateTime.UtcNow;

            _logger.LogInformation("Starting audio recording...");

            // Start recording (runs until cancelled)
            var cts = new CancellationTokenSource();
            await _audioRecorder.StartRecordingAsync(cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start recording");
            _isRecording = false;
            _recordingStartTime = null;
        }
    }

    private async Task StopRecordingAsync()
    {
        if (!_isRecording)
        {
            _logger.LogWarning("No recording in progress");
            return;
        }

        try
        {
            _logger.LogInformation("Stopping audio recording...");

            await _audioRecorder.StopRecordingAsync();

            var recordedData = _audioRecorder.GetRecordedData();
            _logger.LogInformation("Recording stopped. Captured {ByteCount} bytes", recordedData.Length);

            if (recordedData.Length > 0)
            {
                // Sherpa-ONNX processes raw PCM data directly (no WAV conversion needed)
                _logger.LogInformation("Starting transcription...");
                var transcription = await _speechTranscriber.TranscribeAsync(recordedData);

                if (transcription.Success && !string.IsNullOrWhiteSpace(transcription.Text))
                {
                    _logger.LogInformation("Transcription successful: {Text} (confidence: {Confidence:F3})", 
                        transcription.Text, transcription.Confidence);

                    // Type transcribed text
                    await _textTyper.TypeTextAsync(transcription.Text);
                    _logger.LogInformation("Text typed successfully");
                }
                else
                {
                    _logger.LogWarning("Transcription failed or empty: {Error}", transcription.ErrorMessage);
                }
            }
            else
            {
                _logger.LogWarning("No audio data recorded");
            }

            if (_recordingStartTime.HasValue)
            {
                var duration = DateTime.UtcNow - _recordingStartTime.Value;
                _logger.LogInformation("Total recording duration: {Duration:F2}s", duration.TotalSeconds);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop recording");
        }
        finally
        {
            _isRecording = false;
            _recordingStartTime = null;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Dictation service stopping...");

        if (_isRecording)
        {
            await StopRecordingAsync();
        }

        await _keyboardMonitor.StopMonitoringAsync();

        await base.StopAsync(cancellationToken);
    }
}
