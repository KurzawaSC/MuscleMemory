namespace MuscleMemory.Tests.Testing;

public static class RepositoryFiles
{
    public const string AppProjectFolder = "MuscleMemory";

    private const string SolutionFileName = "MuscleMemory.slnx";
    private const string RootNotFoundFormat = "No {0} was found in {1} or any directory above it.";

    private static readonly Lazy<string> LazyRoot = new(FindRoot);
    private static readonly string[] BuildOutputFolders = ["bin", "obj"];

    public static string Root => LazyRoot.Value;

    public static string PathOf(params string[] parts) => Path.Combine([Root, .. parts]);

    public static IEnumerable<string> AppSources(string searchPattern)
    {
        var appFolder = PathOf(AppProjectFolder);
        return Directory.EnumerateFiles(appFolder, searchPattern, SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(Path.GetRelativePath(appFolder, path)));
    }

    public static string RelativePath(string path) => Path.GetRelativePath(Root, path);

    private static bool IsBuildOutput(string relativePath) =>
        relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Intersect(BuildOutputFolders).Any();

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException(string.Format(RootNotFoundFormat, SolutionFileName, AppContext.BaseDirectory));
    }
}
