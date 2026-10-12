using MuscleMemory.Data;
using NSubstitute;
using SQLite;

namespace MuscleMemory.Tests.Testing;

public sealed class TestDatabase : IAsyncDisposable
{
    private const string RootFolderName = "mm-tests";
    private const string CacheFolderName = "cache";
    private const string FailureTriggerFormat =
        "CREATE TRIGGER \"fail_{1}_{0}\" BEFORE {1} ON \"{0}\" BEGIN SELECT RAISE(ABORT, 'injected failure'); END";

    public TestDatabase()
    {
        Folder = Path.Combine(Path.GetTempPath(), RootFolderName, Guid.NewGuid().ToString("N"));
        CacheFolder = Path.Combine(Folder, CacheFolderName);
        Directory.CreateDirectory(CacheFolder);

        FileSystem = Substitute.For<IFileSystem>();
        FileSystem.AppDataDirectory.Returns(Folder);
        FileSystem.CacheDirectory.Returns(CacheFolder);

        Context = new DatabaseContext(FileSystem);
    }

    public static string RootFolder => Path.Combine(Path.GetTempPath(), RootFolderName);

    public string Folder { get; }

    public string CacheFolder { get; }

    public IFileSystem FileSystem { get; }

    public DatabaseContext Context { get; }

    public string FilePath => Context.DatabasePath;

    public async Task<SQLiteAsyncConnection> ConnectionAsync() => await Context.GetConnectionAsync();

    public async Task<int> CountAsync(string table)
    {
        var connection = await ConnectionAsync();
        return await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM \"{table}\"");
    }

    public async Task FailOnAsync(string table, TableOperation operation)
    {
        var connection = await ConnectionAsync();
        await connection.ExecuteAsync(string.Format(FailureTriggerFormat, table, operation.ToString().ToUpperInvariant()));
    }

    public async ValueTask DisposeAsync()
    {
        await (await Context.GetConnectionAsync()).CloseAsync();
        Directory.Delete(Folder, recursive: true);
    }
}
