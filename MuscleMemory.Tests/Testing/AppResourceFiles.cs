using System.Reflection;
using System.Resources;
using MuscleMemory.Resources.Strings;

namespace MuscleMemory.Tests.Testing;

public static class AppResourceFiles
{
    private const string NeutralFileName = "AppResources.resx";
    private const string SatelliteSearchPattern = "AppResources.*.resx";
    private const char NameSeparator = '.';

    private static readonly Lazy<ResourceFile> LazyNeutral = new(LoadNeutral);
    private static readonly Lazy<IReadOnlyList<ResourceFile>> LazySatellites = new(LoadSatellites);

    public static string Folder => RepositoryFiles.PathOf(RepositoryFiles.AppProjectFolder, "Resources", "Strings");

    public static string NeutralLanguage =>
        typeof(AppResources).Assembly.GetCustomAttribute<NeutralResourcesLanguageAttribute>()!.CultureName;

    public static ResourceFile Neutral => LazyNeutral.Value;

    public static IReadOnlyList<ResourceFile> Satellites => LazySatellites.Value;

    public static IEnumerable<ResourceFile> All => Satellites.Prepend(Neutral);

    private static ResourceFile LoadNeutral() => ResourceFile.Load(NeutralLanguage, Path.Combine(Folder, NeutralFileName));

    private static IReadOnlyList<ResourceFile> LoadSatellites() =>
    [
        .. Directory.EnumerateFiles(Folder, SatelliteSearchPattern)
            .Select(path => ResourceFile.Load(Path.GetFileNameWithoutExtension(path).Split(NameSeparator)[^1], path))
            .OrderBy(file => file.Language, StringComparer.Ordinal)
    ];
}
