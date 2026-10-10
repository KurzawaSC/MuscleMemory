using System.Globalization;
using MuscleMemory.Resources.Strings;

namespace MuscleMemory.ViewModels;

public sealed record LanguageOption(CultureInfo? Language, string Label, string ShortLabel)
{
    public static LanguageOption SystemDefault(CultureInfo? systemLanguage) => new(
        null,
        systemLanguage is null
            ? AppResources.LanguageSystemDefault
            : string.Format(CultureInfo.CurrentCulture, AppResources.LanguageSystemDefaultFormat, NativeName(systemLanguage)),
        AppResources.LanguageSystemDefault);

    public static LanguageOption For(CultureInfo language)
    {
        var name = NativeName(language);
        return new(language, name, name);
    }

    public bool Represents(CultureInfo? language) => Equals(Language, language);

    private static string NativeName(CultureInfo language) =>
        language.TextInfo.ToUpper(language.NativeName[0]) + language.NativeName[1..];
}
