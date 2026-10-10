using System.Globalization;

namespace MuscleMemory.Localization;

public static class PluralRules
{
    private const int Ten = 10;
    private const int Hundred = 100;
    private const int Million = 1_000_000;

    private static readonly Dictionary<string, Func<int, PluralCategory>> RulesByLanguage = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cs"] = Czech,
        ["es"] = OneOrMillions,
        ["fr"] = ZeroOrOneOrMillions,
        ["ga"] = Irish,
        ["hr"] = Croatian,
        ["it"] = OneOrMillions,
        ["lt"] = Lithuanian,
        ["lv"] = Latvian,
        ["mt"] = Maltese,
        ["pl"] = Polish,
        ["pt"] = ZeroOrOneOrMillions,
        ["pt-PT"] = OneOrMillions,
        ["ro"] = Romanian,
        ["ru"] = EastSlavic,
        ["sk"] = Czech,
        ["sl"] = Slovenian,
        ["uk"] = EastSlavic
    };

    public static PluralCategory Select(int count, CultureInfo culture) => RuleFor(culture)(Math.Abs(count));

    private static Func<int, PluralCategory> RuleFor(CultureInfo culture) =>
        RulesByLanguage.GetValueOrDefault(culture.Name)
        ?? RulesByLanguage.GetValueOrDefault(culture.TwoLetterISOLanguageName)
        ?? OneOrOther;

    private static PluralCategory OneOrOther(int n) => n == 1 ? PluralCategory.One : PluralCategory.Other;

    private static PluralCategory OneOrMillions(int n) => n switch
    {
        1 => PluralCategory.One,
        _ when IsWholeMillions(n) => PluralCategory.Many,
        _ => PluralCategory.Other
    };

    private static PluralCategory ZeroOrOneOrMillions(int n) => n switch
    {
        0 or 1 => PluralCategory.One,
        _ when IsWholeMillions(n) => PluralCategory.Many,
        _ => PluralCategory.Other
    };

    private static PluralCategory Czech(int n) => n switch
    {
        1 => PluralCategory.One,
        >= 2 and <= 4 => PluralCategory.Few,
        _ => PluralCategory.Other
    };

    private static PluralCategory Polish(int n) => n switch
    {
        1 => PluralCategory.One,
        _ when EndsInTwoToFour(n) => PluralCategory.Few,
        _ => PluralCategory.Many
    };

    private static PluralCategory EastSlavic(int n) => n switch
    {
        _ when EndsInOne(n) => PluralCategory.One,
        _ when EndsInTwoToFour(n) => PluralCategory.Few,
        _ => PluralCategory.Many
    };

    private static PluralCategory Croatian(int n) => n switch
    {
        _ when EndsInOne(n) => PluralCategory.One,
        _ when EndsInTwoToFour(n) => PluralCategory.Few,
        _ => PluralCategory.Other
    };

    private static PluralCategory Lithuanian(int n) => n switch
    {
        _ when IsTeen(n) => PluralCategory.Other,
        _ when n % Ten == 1 => PluralCategory.One,
        _ when n % Ten >= 2 => PluralCategory.Few,
        _ => PluralCategory.Other
    };

    private static PluralCategory Latvian(int n) => n switch
    {
        _ when n % Ten == 0 || IsTeen(n) => PluralCategory.Zero,
        _ when n % Ten == 1 => PluralCategory.One,
        _ => PluralCategory.Other
    };

    private static PluralCategory Romanian(int n) => n switch
    {
        1 => PluralCategory.One,
        0 => PluralCategory.Few,
        _ when n % Hundred is >= 1 and <= 19 => PluralCategory.Few,
        _ => PluralCategory.Other
    };

    private static PluralCategory Slovenian(int n) => (n % Hundred) switch
    {
        1 => PluralCategory.One,
        2 => PluralCategory.Two,
        3 or 4 => PluralCategory.Few,
        _ => PluralCategory.Other
    };

    private static PluralCategory Irish(int n) => n switch
    {
        1 => PluralCategory.One,
        2 => PluralCategory.Two,
        >= 3 and <= 6 => PluralCategory.Few,
        >= 7 and <= 10 => PluralCategory.Many,
        _ => PluralCategory.Other
    };

    private static PluralCategory Maltese(int n) => n switch
    {
        1 => PluralCategory.One,
        2 => PluralCategory.Two,
        0 => PluralCategory.Few,
        _ when n % Hundred is >= 3 and <= 10 => PluralCategory.Few,
        _ when IsTeen(n) => PluralCategory.Many,
        _ => PluralCategory.Other
    };

    private static bool EndsInOne(int n) => n % Ten == 1 && n % Hundred != 11;

    private static bool EndsInTwoToFour(int n) => n % Ten is >= 2 and <= 4 && n % Hundred is not (>= 12 and <= 14);

    private static bool IsTeen(int n) => n % Hundred is >= 11 and <= 19;

    private static bool IsWholeMillions(int n) => n != 0 && n % Million == 0;
}
