using System.Text.RegularExpressions;

namespace MuscleMemory.Extensions;

public static partial class EnumDisplay
{
    public static string ToDisplayName(this Enum value) =>
        WordBoundary().Replace(value.ToString(), " $1");

    public static TEnum Parse<TEnum>(string displayName) where TEnum : struct, Enum =>
        Enum.Parse<TEnum>(displayName.Replace(" ", string.Empty));

    [GeneratedRegex("(?<!^)([A-Z])")]
    private static partial Regex WordBoundary();
}
