using System.ComponentModel;
using System.Reflection;

namespace VoiceAssistant.Shared.Extensions;

/// <summary>
/// Extension methods for enums.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Gets the Description attribute value for an enum value.
    /// </summary>
    /// <param name="value">The enum value.</param>
    /// <returns>The description if found, otherwise null.</returns>
    public static string? GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description;
    }

    /// <summary>
    /// Gets the Description attribute value for an enum value, 
    /// falling back to the enum name if no description is found.
    /// </summary>
    /// <param name="value">The enum value.</param>
    /// <returns>The description if found, otherwise the enum name.</returns>
    public static string GetDescriptionOrName(this Enum value)
    {
        return value.GetDescription() ?? value.ToString();
    }
}
