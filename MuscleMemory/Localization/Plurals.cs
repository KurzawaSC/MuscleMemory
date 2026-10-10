using System.Globalization;
using MuscleMemory.Resources.Strings;

namespace MuscleMemory.Localization;

public static class Plurals
{
    private const string OtherSuffix = "_Other";
    private const string CategoryKeyFormat = "{0}_{1}";
    private const string NotOtherKeyMessage = "A plural is looked up by its _Other key.";

    public static string Format(string otherKey, int count)
    {
        if (!otherKey.EndsWith(OtherSuffix, StringComparison.Ordinal))
        {
            throw new ArgumentException(NotOtherKeyMessage, nameof(otherKey));
        }

        var category = PluralRules.Select(count, CultureInfo.CurrentUICulture);
        var categoryKey = string.Format(CultureInfo.InvariantCulture, CategoryKeyFormat, otherKey[..^OtherSuffix.Length], category);
        var pattern = AppResources.ResourceManager.GetString(categoryKey) ?? LocalizedText.Instance[otherKey];
        return string.Format(CultureInfo.CurrentCulture, pattern, count);
    }
}
