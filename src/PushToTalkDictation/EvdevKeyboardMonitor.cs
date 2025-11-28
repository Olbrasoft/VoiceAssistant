using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Olbrasoft.VoiceAssistant.PushToTalkDictation;

/// <summary>
/// Linux evdev-based keyboard monitor.
/// Reads keyboard events directly from /dev/input/eventX devices.
/// </summary>
public class EvdevKeyboardMonitor : IKeyboardMonitor
{
    private readonly ILogger<EvdevKeyboardMonitor> _logger;
    private readonly string _devicePath;
    private FileStream? _deviceStream;
    private bool _isMonitoring;
    private bool _disposed;
    private Task? _monitorTask;
    private CancellationTokenSource? _cts;

    // Linux input_event structure (24 bytes)
    // struct input_event {
    //     struct timeval time;  // 16 bytes (tv_sec: 8, tv_usec: 8)
    //     __u16 type;           // 2 bytes
    //     __u16 code;           // 2 bytes
    //     __s32 value;          // 4 bytes
    // };
    private const int InputEventSize = 24;
    private const ushort EV_KEY = 1;  // Key press/release event type
    private const int KEY_PRESS = 1;
    private const int KEY_RELEASE = 0;



    /// <summary>
    /// Initializes a new instance of the <see cref="EvdevKeyboardMonitor"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="devicePath">Path to input device (default: auto-detect keyboard).</param>
    public EvdevKeyboardMonitor(ILogger<EvdevKeyboardMonitor> logger, string? devicePath = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _devicePath = devicePath ?? FindKeyboardDevice();
    }

    /// <inheritdoc/>
    public event EventHandler<KeyEventArgs>? KeyPressed;

    /// <inheritdoc/>
    public event EventHandler<KeyEventArgs>? KeyReleased;

    /// <inheritdoc/>
    public bool IsMonitoring => _isMonitoring;

    /// <inheritdoc/>
    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EvdevKeyboardMonitor));

        if (_isMonitoring)
        {
            _logger.LogWarning("Keyboard monitoring is already active");
            return;
        }

        try
        {
            _logger.LogInformation("Opening keyboard device: {DevicePath}", _devicePath);

            // Open device in read-only mode (synchronous for device files)
            _deviceStream = new FileStream(
                _devicePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize: 24,
                useAsync: false);

            // NOTE: EVIOCGRAB is NOT used because it would block ALL keys on the device.
            // We read events in shared mode - both our app and X.org receive events.
            // This is fine for ScrollLock/CapsLock monitoring.
            _logger.LogInformation("Reading keyboard events in shared mode (X.org also receives events)");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _isMonitoring = true;

            _logger.LogInformation("Keyboard monitoring started");

            // Start monitoring in background
            _monitorTask = Task.Run(() => MonitorEventsAsync(_cts.Token), _cts.Token);

            // Don't await - let it run in background
            // await _monitorTask;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Permission denied. Add user to 'input' group: sudo usermod -a -G input $USER");
            _isMonitoring = false;
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Keyboard monitoring was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start keyboard monitoring");
            _isMonitoring = false;
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task StopMonitoringAsync()
    {
        if (!_isMonitoring)
        {
            _logger.LogWarning("Keyboard monitoring is not active");
            return;
        }

        try
        {
            _isMonitoring = false;
            _cts?.Cancel();

            if (_monitorTask != null)
            {
                await _monitorTask;
            }

            if (_deviceStream != null)
            {
                await _deviceStream.DisposeAsync();
                _deviceStream = null;
            }

            _logger.LogInformation("Keyboard monitoring stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping keyboard monitoring");
            throw;
        }
    }

    private Task MonitorEventsAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[InputEventSize];

        try
        {
            _logger.LogInformation("Starting event monitoring loop (synchronous read)");
            
            while (_isMonitoring && !cancellationToken.IsCancellationRequested)
            {
                // Use synchronous Read for device files (blocking read)
                // This properly waits for kernel events
                int bytesRead = _deviceStream!.Read(buffer, 0, InputEventSize);

                if (bytesRead != InputEventSize)
                {
                    _logger.LogWarning("Incomplete event data received: {BytesRead} bytes", bytesRead);
                    continue;
                }

                // Parse input_event structure
                ParseInputEvent(buffer);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
            _logger.LogInformation("Event monitoring cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading keyboard events");
        }
        
        _logger.LogInformation("Event monitoring loop ended");
        return Task.CompletedTask;
    }

    private void ParseInputEvent(byte[] buffer)
    {
        // Skip timeval (first 16 bytes)
        int offset = 16;

        // Read type (2 bytes)
        ushort type = BitConverter.ToUInt16(buffer, offset);
        offset += 2;

        // Read code (2 bytes) - this is the key code
        ushort code = BitConverter.ToUInt16(buffer, offset);
        offset += 2;

        // Read value (4 bytes) - 0=release, 1=press, 2=repeat
        int value = BitConverter.ToInt32(buffer, offset);

        // Only process key events
        if (type != EV_KEY)
            return;

        // Ignore key repeat events
        if (value != KEY_PRESS && value != KEY_RELEASE)
            return;

        // Convert code to KeyCode enum
        var keyCode = Enum.IsDefined(typeof(KeyCode), (int)code)
            ? (KeyCode)code
            : KeyCode.Unknown;

        var eventArgs = new KeyEventArgs(keyCode, code, DateTime.UtcNow);

        if (value == KEY_PRESS)
        {
            _logger.LogDebug("Key pressed: {KeyCode} (raw code: {RawCode})", keyCode, code);
            KeyPressed?.Invoke(this, eventArgs);
        }
        else if (value == KEY_RELEASE)
        {
            _logger.LogDebug("Key released: {KeyCode} (raw code: {RawCode})", keyCode, code);
            KeyReleased?.Invoke(this, eventArgs);
        }
    }

    private static string FindKeyboardDevice()
    {
        // Try to find keyboard device in /dev/input/by-path/
        var byPathDir = "/dev/input/by-path";

        if (Directory.Exists(byPathDir))
        {
            var kbdDevices = Directory.GetFiles(byPathDir, "*kbd");
            if (kbdDevices.Length > 0)
            {
                return kbdDevices[0];
            }
        }

        // Fallback: try common event devices
        for (int i = 0; i < 10; i++)
        {
            var devicePath = $"/dev/input/event{i}";
            if (File.Exists(devicePath))
            {
                return devicePath;
            }
        }

        throw new FileNotFoundException("No keyboard input device found. Check /dev/input/");
    }

    /// <inheritdoc/>
    public bool IsCapsLockOn()
    {
        try
        {
            // Find CapsLock LED in /sys/class/leds/
            var ledsDir = "/sys/class/leds";
            if (Directory.Exists(ledsDir))
            {
                var capsLockLed = Directory.GetDirectories(ledsDir)
                    .FirstOrDefault(d => d.Contains("capslock", StringComparison.OrdinalIgnoreCase));

                if (capsLockLed != null)
                {
                    var brightnessPath = Path.Combine(capsLockLed, "brightness");
                    if (File.Exists(brightnessPath))
                    {
                        var value = File.ReadAllText(brightnessPath).Trim();
                        var isOn = value != "0";
                        _logger.LogDebug("CapsLock LED state: {State} (brightness: {Value})", isOn ? "ON" : "OFF", value);
                        return isOn;
                    }
                }
            }

            _logger.LogWarning("Could not find CapsLock LED in /sys/class/leds/");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading CapsLock state");
            return false;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        if (_isMonitoring)
        {
            StopMonitoringAsync().GetAwaiter().GetResult();
        }

        _cts?.Dispose();
        _deviceStream?.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
