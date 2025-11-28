using System;
using System.IO;

Console.WriteLine("=== Key Code Grabber ===");
Console.WriteLine("Monitoring keyboard events from /dev/input/event2");
Console.WriteLine("Press keys to see their codes. Press Ctrl+C to exit.");
Console.WriteLine();

const string devicePath = "/dev/input/event2";
const int InputEventSize = 24;
const ushort EV_KEY = 1;

try
{
    using var stream = new FileStream(devicePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    var buffer = new byte[InputEventSize];

    while (true)
    {
        int bytesRead = stream.Read(buffer, 0, InputEventSize);

        if (bytesRead != InputEventSize)
            continue;

        // Parse input_event structure
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
            continue;

        string action = value switch
        {
            0 => "RELEASE",
            1 => "PRESS  ",
            2 => "REPEAT ",
            _ => "UNKNOWN"
        };

        // Highlight CapsLock and ScrollLock
        string highlight = code switch
        {
            58 => " <<<< CAPS LOCK!",
            70 => " <<<< SCROLL LOCK!",
            _ => ""
        };

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {action} | Code: {code,3}{highlight}");
    }
}
catch (UnauthorizedAccessException)
{
    Console.WriteLine($"ERROR: Permission denied accessing {devicePath}");
    Console.WriteLine("Run: sudo usermod -a -G input $USER");
    Console.WriteLine("Then log out and log back in.");
    return 1;
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: {ex.Message}");
    return 1;
}
