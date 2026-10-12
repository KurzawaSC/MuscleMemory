namespace MuscleMemory.Tests.Testing;

public static class RepositoryFiles
{
    private const string SolutionFileName = "MuscleMemory.slnx";
    private const string RootNotFoundFormat = "No {0} was found in {1} or any directory above it.";

    private static readonly Lazy<string> LazyRoot = new(FindRoot);

    public static string Root => LazyRoot.Value;

    public static string PathOf(params string[] parts) => Path.Combine([Root, .. parts]);

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
