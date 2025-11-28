using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Olbrasoft.VoiceAssistant.Shared.Input;

/// <summary>
/// Cross-platform utility for checking CapsLock state.
/// Works on Windows (via Win32 API) and Linux (via sysfs LED state).
/// </summary>
public static class CapsLockState
{
    // Windows P/Invoke
    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    private static extern short GetKeyState(int keyCode);

    private const int VK_CAPITAL = 0x14;

    // Cache the Linux LED path to avoid repeated filesystem searches
    private static string? _linuxLedPath;
    private static bool _linuxLedPathSearched;

    /// <summary>
    /// Checks if CapsLock is currently ON.
    /// </summary>
    /// <returns>True if CapsLock is ON, false otherwise.</returns>
    public static bool IsOn()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return IsOnWindows();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return IsOnLinux();
        }

        // macOS and other platforms not supported
        throw new PlatformNotSupportedException(
            "CapsLock state detection is not supported on this platform.");
    }

    /// <summary>
    /// Tries to check if CapsLock is ON, returning false on error instead of throwing.
    /// </summary>
    /// <param name="isOn">True if CapsLock is ON.</param>
    /// <returns>True if the state was successfully determined, false otherwise.</returns>
    public static bool TryIsOn(out bool isOn)
    {
        try
        {
            isOn = IsOn();
            return true;
        }
        catch
        {
            isOn = false;
            return false;
        }
    }

    private static bool IsOnWindows()
    {
        // The least significant bit indicates the toggle state
        return (GetKeyState(VK_CAPITAL) & 1) != 0;
    }

    private static bool IsOnLinux()
    {
        // Try to find and cache the LED path
        if (!_linuxLedPathSearched)
        {
            _linuxLedPath = FindLinuxCapsLockLedPath();
            _linuxLedPathSearched = true;
        }

        if (_linuxLedPath == null)
        {
            throw new InvalidOperationException(
                "CapsLock LED not found in /sys/class/leds/. " +
                "This may happen on systems without keyboard LED support.");
        }

        // Read the brightness file directly (no need to spawn bash)
        var brightnessPath = Path.Combine(_linuxLedPath, "brightness");
        var content = File.ReadAllText(brightnessPath).Trim();
        return content == "1";
    }

    private static string? FindLinuxCapsLockLedPath()
    {
        const string ledsPath = "/sys/class/leds";
        
        if (!Directory.Exists(ledsPath))
            return null;

        // Look for any LED containing "capslock" in its name
        foreach (var dir in Directory.GetDirectories(ledsPath))
        {
            var name = Path.GetFileName(dir);
            if (name.Contains("capslock", StringComparison.OrdinalIgnoreCase))
            {
                // Verify the brightness file exists
                var brightnessPath = Path.Combine(dir, "brightness");
                if (File.Exists(brightnessPath))
                {
                    return dir;
                }
            }
        }

        return null;
    }
}
