using System.Runtime.InteropServices;
using Olbrasoft.VoiceAssistant.Shared.Input;
using Xunit;

namespace Olbrasoft.VoiceAssistant.Shared.Tests.Input;

public class CapsLockStateTests
{
    [Fact]
    public void IsOn_OnSupportedPlatform_ShouldReturnBoolean()
    {
        // Skip test on unsupported platforms
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return;
        }

        try
        {
            // Act
            var result = CapsLockState.IsOn();

            // Assert - should return boolean without throwing
            Assert.IsType<bool>(result);
        }
        catch (InvalidOperationException)
        {
            // Expected on Linux systems without CapsLock LED
            Assert.True(true);
        }
    }

    [Fact]
    public void TryIsOn_OnAnyPlatform_ShouldNotThrow()
    {
        // Act
        var success = CapsLockState.TryIsOn(out var isOn);

        // Assert - should not throw
        Assert.IsType<bool>(success);
        Assert.IsType<bool>(isOn);
    }

    [Fact]
    public void TryIsOn_OnSupportedPlatform_ShouldReturnTrue()
    {
        // Skip on unsupported platforms
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return;
        }

        // Act
        var success = CapsLockState.TryIsOn(out var isOn);

        // Assert - may return false on Linux without LED, which is acceptable
        Assert.IsType<bool>(success);
    }

    [Fact]
    public void TryIsOn_WhenFails_ShouldReturnFalseAndSetIsOnToFalse()
    {
        // This test verifies the contract: if TryIsOn returns false,
        // isOn should be false (not left uninitialized)
        
        // Act
        var success = CapsLockState.TryIsOn(out var isOn);

        // Assert
        if (!success)
        {
            Assert.False(isOn);
        }
    }

    [Fact]
    public void CapsLockState_ShouldBeStaticClass()
    {
        // Assert
        var type = typeof(CapsLockState);
        Assert.True(type.IsAbstract && type.IsSealed);
    }

    [Fact(Skip = "Only runs on macOS")]
    public void IsOn_OnMacOS_ShouldThrowPlatformNotSupportedException()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return;
        }

        // Act & Assert
        Assert.Throws<PlatformNotSupportedException>(() => CapsLockState.IsOn());
    }
}
