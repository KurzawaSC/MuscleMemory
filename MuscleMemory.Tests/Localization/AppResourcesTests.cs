using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using MuscleMemory.Localization;
using MuscleMemory.Resources.Strings;
using MuscleMemory.Tests.Testing;

namespace MuscleMemory.Tests.Localization;

public sealed partial class AppResourcesTests
{
    private const string SatelliteLanguagesKey = "SatelliteLanguages";
    private const char LanguageSeparator = ';';
    private const string XamlSearchPattern = "*.xaml";
    private const string CSharpSearchPattern = "*.cs";
    private const string KeyGroup = "key";
    private const string PrefixGroup = "prefix";
    private const string EscapedOpenBrace = "{{";
    private const string EscapedCloseBrace = "}}";
    private const string PluralFamilyMarker = "_*";
    private const int SampledCounts = 1_000;
    private const int Million = 1_000_000;

    private static readonly int[] PluralSamples = [.. Enumerable.Range(0, SampledCounts), Million, 2 * Million];

    private static readonly HashSet<string> OneOrOtherLanguages = ["bg", "da", "de", "el", "en", "et", "fi", "hu", "nl", "sv"];

    [Fact]
    public void Satellites_Keys_MatchTheNeutralKeysAndPluralFamilies()
    {
        var neutralKeys = KeyIdentities(AppResourceFiles.Neutral);

        var violations = AppResourceFiles.Satellites.SelectMany(satellite =>
        {
            var satelliteKeys = KeyIdentities(satellite);
            return neutralKeys.Except(satelliteKeys).Select(key => $"{satellite.FileName} is missing {key}")
                .Concat(satelliteKeys.Except(neutralKeys).Select(key => $"{satellite.FileName} has orphan {key}"));
        });

        Assert.NotEmpty(AppResourceFiles.Satellites);
        Violations.AssertNone(violations);
    }

    [Fact]
    public void AllFiles_Values_AreNotEmpty()
    {
        var violations =
            from file in AppResourceFiles.All
            from entry in file.Values
            where string.IsNullOrWhiteSpace(entry.Value)
            select $"{file.FileName}: {entry.Key} is empty";

        Violations.AssertNone(violations);
    }

    [Fact]
    public void AllFiles_Placeholders_MatchTheNeutralValue()
    {
        var neutral = AppResourceFiles.Neutral.Values;

        var violations =
            from file in AppResourceFiles.All
            from entry in file.Values
            let referenceKey = ReferenceKey(entry.Key)
            where neutral.ContainsKey(referenceKey)
            let expected = Placeholders(neutral[referenceKey])
            let actual = Placeholders(entry.Value)
            where !actual.SetEquals(expected)
            select $"{file.FileName}: {entry.Key} has [{string.Join(' ', actual.Order())}], {AppResourceFiles.Neutral.FileName}: {referenceKey} has [{string.Join(' ', expected.Order())}]";

        Violations.AssertNone(violations);
    }

    [Fact]
    public void AllFiles_PluralFamilies_HaveExactlyTheCategoriesTheLanguageSelects()
    {
        var families = AppResourceFiles.All
            .SelectMany(file => file.Values.Keys)
            .Select(ResourceFile.PluralFamily)
            .OfType<string>()
            .ToHashSet();

        var violations =
            from file in AppResourceFiles.All
            let expected = CategoriesSelectedFor(file.Language)
            from family in families
            let actual = file.Values.Keys.Where(key => ResourceFile.PluralFamily(key) == family).Select(ResourceFile.PluralCategoryOf).ToHashSet()
            from message in expected.Except(actual).Select(category => $"{file.FileName}: {family} is missing {category}")
                .Concat(actual.Except(expected).Select(category => $"{file.FileName}: {family} has {category}, which {file.Language} never selects"))
            select message;

        Assert.NotEmpty(families);
        Violations.AssertNone(violations);
    }

    [Fact]
    public void SatelliteLanguages_Metadata_ListsExactlyTheSatelliteFilesAsValidCulturesWithPluralRules()
    {
        var listed = typeof(AppResources).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(attribute => attribute.Key == SatelliteLanguagesKey)
            .SelectMany(attribute => (attribute.Value ?? string.Empty).Split(LanguageSeparator, StringSplitOptions.RemoveEmptyEntries))
            .ToHashSet();
        var files = AppResourceFiles.Satellites.Select(file => file.Language).ToHashSet();

        var violations = files.Except(listed).Select(language => $"{language} has a satellite file but is not in the {SatelliteLanguagesKey} metadata")
            .Concat(listed.Except(files).Select(language => $"{language} is in the {SatelliteLanguagesKey} metadata but has no satellite file"))
            .Concat(listed.Prepend(AppResourceFiles.NeutralLanguage).SelectMany(LanguageViolations));

        Assert.NotEmpty(listed);
        Violations.AssertNone(violations);
    }

    [Fact]
    public void XamlFiles_TextKeys_ExistInTheNeutralResources()
    {
        var usages = XamlKeyUsages().ToList();

        var violations =
            from usage in usages
            where !AppResourceFiles.Neutral.Values.ContainsKey(usage.Key)
            select $"{usage.File}: {usage.Key} is not in {AppResourceFiles.Neutral.FileName}";

        Assert.NotEmpty(usages);
        Violations.AssertNone(violations);
    }

    [Fact]
    public void AllFiles_Keys_AreEachReferencedFromXamlCSharpOrAPluralLookup()
    {
        var referenced = XamlKeyUsages().Select(usage => usage.Key)
            .Concat(RepositoryFiles.AppSources(CSharpSearchPattern).SelectMany(path =>
                AccessorUsage().Matches(File.ReadAllText(path)).Select(match => match.Groups[KeyGroup].Value)))
            .ToHashSet();
        var referencedFamilies = referenced.Select(ResourceFile.PluralFamily).OfType<string>().ToHashSet();

        var violations =
            from file in AppResourceFiles.All
            from key in file.Values.Keys
            where !referenced.Contains(key) && !(ResourceFile.PluralFamily(key) is { } family && referencedFamilies.Contains(family))
            select $"{file.FileName}: {key} is never used";

        Violations.AssertNone(violations);
    }

    private static HashSet<string> KeyIdentities(ResourceFile file) =>
        [.. file.Values.Keys.Select(key => ResourceFile.PluralFamily(key) is { } family ? family + PluralFamilyMarker : key)];

    private static string ReferenceKey(string key) =>
        ResourceFile.PluralFamily(key) is { } family ? ResourceFile.PluralKey(family, PluralCategory.Other) : key;

    private static HashSet<string> Placeholders(string value) =>
        [.. Placeholder().Matches(value.Replace(EscapedOpenBrace, string.Empty).Replace(EscapedCloseBrace, string.Empty)).Select(match => match.Value)];

    private static HashSet<PluralCategory> CategoriesSelectedFor(string language)
    {
        var culture = CultureInfo.GetCultureInfo(language);
        return [.. PluralSamples.Select(count => PluralRules.Select(count, culture)).Append(PluralCategory.Other)];
    }

    private static IEnumerable<string> LanguageViolations(string language)
    {
        CultureInfo culture;
        try
        {
            culture = CultureInfo.GetCultureInfo(language, predefinedOnly: true);
        }
        catch (CultureNotFoundException)
        {
            return [$"{language} is not a valid culture"];
        }

        return HasOwnPluralRule(culture) || OneOrOtherLanguages.Contains(culture.TwoLetterISOLanguageName)
            ? []
            : [$"{language} has no rule in PluralRules and is not a documented one/other language"];
    }

    private static bool HasOwnPluralRule(CultureInfo culture) =>
        PluralSamples.Any(count => PluralRules.Select(count, culture) != PluralRules.Select(count, CultureInfo.InvariantCulture));

    private static IEnumerable<(string File, string Key)> XamlKeyUsages() =>
        from path in RepositoryFiles.AppSources(XamlSearchPattern)
        let xaml = File.ReadAllText(path)
        from prefix in LocalizationPrefix().Matches(xaml).Select(match => match.Groups[PrefixGroup].Value)
        from match in TextUsage(prefix).Matches(xaml)
        select (RepositoryFiles.RelativePath(path), match.Groups[KeyGroup].Value);

    private static Regex TextUsage(string prefix) =>
        new($@"\{{{Regex.Escape(prefix)}:Text\s+(?:Key\s*=\s*)?(?<{KeyGroup}>\w+)\s*\}}");

    [GeneratedRegex(@"xmlns:(?<prefix>\w+)=""clr-namespace:MuscleMemory\.Localization""")]
    private static partial Regex LocalizationPrefix();

    [GeneratedRegex(@"\bAppResources\.(?<key>\w+)")]
    private static partial Regex AccessorUsage();

    [GeneratedRegex(@"\{\d+(?:,-?\d+)?(?::[^{}]*)?\}")]
    private static partial Regex Placeholder();
}
