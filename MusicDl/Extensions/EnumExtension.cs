using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MusicDl.Extensions;

public static class EnumExtension
{
    /// <summary>
    /// Gets the description attribute value of an enum value, or the enum name if no description is found.
    /// </summary>
    /// <param name="enumValue">The enum value to get description for.</param>
    /// <returns>The description text or enum name.</returns>
    public static string GetDescription(this Enum enumValue)
    {
        ArgumentNullException.ThrowIfNull(enumValue);

        var enumType = enumValue.GetType();
        var enumName = enumValue.ToString();

        var field = enumType.GetField(enumName);
        if (field is null)
            return enumName;

        var description = field.GetCustomAttribute<DescriptionAttribute>()?.Description;
        return description ?? enumName;
    }

    /// <summary>
    /// Gets the description attribute value of an enum value with caching for better performance.
    /// </summary>
    /// <param name="enumValue">The enum value to get description for.</param>
    /// <returns>The description text or enum name.</returns>
    public static string GetDescriptionCached(this Enum enumValue)
    {
        ArgumentNullException.ThrowIfNull(enumValue);

        return DescriptionCache.GetOrAdd(enumValue, e => e.GetDescription());
    }
}

/// <summary>
/// Internal cache for enum descriptions to improve performance.
/// </summary>
file static class DescriptionCache
{
    private static readonly ConditionalWeakTable<Enum, string> _cache = [];

    public static string GetOrAdd(Enum enumValue, Func<Enum, string> factory)
    {
        return _cache.GetValue(enumValue, new ConditionalWeakTable<Enum, string>.CreateValueCallback(factory));
    }
}