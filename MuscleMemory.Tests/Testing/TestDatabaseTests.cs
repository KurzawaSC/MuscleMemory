using MuscleMemory.Constants;
using MuscleMemory.Models;
using SQLite;

namespace MuscleMemory.Tests.Testing;

public sealed class TestDatabaseTests
{
    private const string SqliteVersionSql = "SELECT sqlite_version()";

    [Fact]
    public async Task Constructor_NewDatabase_UsesOwnFolderUnderTempRoot()
    {
        await using var database = new TestDatabase();

        Assert.Equal(TestDatabase.RootFolder, Path.GetDirectoryName(database.Folder));
        Assert.Equal(Path.Combine(database.Folder, DatabaseNames.DatabaseFileName), database.FilePath);
        Assert.True(Directory.Exists(database.CacheFolder));
    }

    [Fact]
    public async Task ConnectionAsync_FirstUse_LoadsNativeSqliteAndCreatesFile()
    {
        await using var database = new TestDatabase();

        var connection = await database.ConnectionAsync();

        Assert.False(string.IsNullOrEmpty(await connection.ExecuteScalarAsync<string>(SqliteVersionSql)));
        Assert.True(File.Exists(database.FilePath));
    }

    [Fact]
    public async Task DisposeAsync_AfterWrites_DeletesFolder()
    {
        var database = new TestDatabase();
        var connection = await database.ConnectionAsync();
        await connection.InsertAsync(new Exercise { Name = nameof(DisposeAsync_AfterWrites_DeletesFolder) });
        File.WriteAllText(Path.Combine(database.CacheFolder, DatabaseNames.ExportFileName), string.Empty);

        await database.DisposeAsync();

        Assert.False(Directory.Exists(database.Folder));
    }

    [Fact]
    public async Task DisposeAsync_NeverOpened_DeletesFolder()
    {
        var database = new TestDatabase();

        await database.DisposeAsync();

        Assert.False(Directory.Exists(database.Folder));
    }

    [Fact]
    public async Task ConnectionAsync_TwoDatabases_KeepRowsApart()
    {
        await using var first = new TestDatabase();
        await using var second = new TestDatabase();

        await (await first.ConnectionAsync()).InsertAsync(new Exercise { Name = nameof(first) });

        Assert.Equal(1, await first.CountAsync(nameof(Exercise)));
        Assert.Equal(0, await second.CountAsync(nameof(Exercise)));
    }

    [Fact]
    public async Task FailOnAsync_Insert_AbortsTheStatement()
    {
        await using var database = new TestDatabase();
        await database.FailOnAsync(nameof(Exercise), TableOperation.Insert);
        var connection = await database.ConnectionAsync();

        await Assert.ThrowsAsync<SQLiteException>(() => connection.InsertAsync(new Exercise { Name = nameof(Exercise) }));
        Assert.Equal(0, await database.CountAsync(nameof(Exercise)));
    }
}
