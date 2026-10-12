using MuscleMemory.Constants;
using MuscleMemory.Data;
using MuscleMemory.Models;
using MuscleMemory.Tests.Testing;
using NSubstitute;
using SQLite;

namespace MuscleMemory.Tests.Data;

public sealed class DatabaseContextTests : IAsyncLifetime
{
    private const string MissingFolderName = "missing";
    private const string TablesSql = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
    private const string IndexesSql = "SELECT tbl_name || '.' || name FROM sqlite_master WHERE type = 'index' AND sql IS NOT NULL ORDER BY name";
    private const string TableSqlFormat = "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = '{0}'";
    private const string AutoIncrement = "autoincrement";
    private const string ColumnsSql = "SELECT name AS Name, type AS Type, pk > 0 AS IsKey FROM pragma_table_info(?) ORDER BY cid";

    private static readonly string NameColumn = $"Name varchar({DomainDefaults.MaxNameLength})";

    private readonly TestDatabase _database = new();

    public static TheoryData<string, string[]> ExpectedColumns => new()
    {
        { nameof(Exercise), ["Id integer pk", NameColumn, "TargetMuscleGroup integer", "Equipment integer"] },
        { nameof(Workout), ["Id integer pk", NameColumn] },
        {
            nameof(WorkoutExercise),
            ["Id integer pk", "WorkoutId integer", "ExerciseId integer", "Order integer", "Sets integer", "Reps integer",
             "BreakTimeInSeconds integer", "ExerciseName varchar", "TargetRPE integer"]
        },
        { nameof(WorkoutSession), ["Id integer pk", "WorkoutId integer", "WorkoutName varchar", "StartTimeUtc bigint", "EndTimeUtc bigint"] },
        {
            nameof(SessionExercise),
            ["Id integer pk", "WorkoutSessionId integer", "ExerciseId integer", "ExerciseName varchar", "Order integer",
             "PlannedSets integer", "PlannedReps integer", "BreakTimeInSeconds integer", "TargetRPE integer"]
        },
        { nameof(WorkoutSet), ["Id integer pk", "SessionExerciseId integer", "Weight float", "Reps integer", "SetNumber integer"] },
        {
            nameof(ActiveWorkoutState),
            ["Id integer pk", "SessionId integer", "StartTimeUtc bigint", "CurrentExerciseIndex integer", "IsResting integer",
             "BreakEndTimeUtc bigint", "RestDurationSeconds integer"]
        }
    };

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    [Fact]
    public void DatabasePath_AppDataDirectory_PointsAtDatabaseFile() =>
        Assert.Equal(Path.Combine(_database.Folder, DatabaseNames.DatabaseFileName), _database.Context.DatabasePath);

    [Fact]
    public async Task GetConnectionAsync_EmptyFolder_CreatesEveryTable()
    {
        Assert.False(File.Exists(_database.FilePath));

        var connection = await _database.Context.GetConnectionAsync();

        Assert.True(File.Exists(_database.FilePath));
        Assert.Equal(
            DatabaseContext.Tables.Select(table => table.Name).Order(StringComparer.Ordinal),
            await connection.QueryScalarsAsync<string>(TablesSql));
    }

    [Theory]
    [MemberData(nameof(ExpectedColumns))]
    public async Task GetConnectionAsync_EmptyFolder_CreatesExpectedColumns(string table, string[] expected)
    {
        var connection = await _database.Context.GetConnectionAsync();

        var columns = await connection.QueryAsync<TableColumn>(ColumnsSql, table);

        Assert.Equal(expected, columns.Select(Describe));
    }

    [Fact]
    public async Task GetConnectionAsync_EmptyFolder_IndexesEveryForeignKeyColumn()
    {
        var connection = await _database.Context.GetConnectionAsync();

        Assert.Equal(
            [
                "SessionExercise.SessionExercise_ExerciseId",
                "SessionExercise.SessionExercise_WorkoutSessionId",
                "WorkoutExercise.WorkoutExercise_ExerciseId",
                "WorkoutExercise.WorkoutExercise_WorkoutId",
                "WorkoutSession.WorkoutSession_WorkoutId",
                "WorkoutSet.WorkoutSet_SessionExerciseId"
            ],
            await connection.QueryScalarsAsync<string>(IndexesSql));
    }

    [Theory]
    [InlineData(nameof(Exercise), true)]
    [InlineData(nameof(Workout), true)]
    [InlineData(nameof(WorkoutExercise), true)]
    [InlineData(nameof(WorkoutSession), true)]
    [InlineData(nameof(SessionExercise), true)]
    [InlineData(nameof(WorkoutSet), true)]
    [InlineData(nameof(ActiveWorkoutState), false)]
    public async Task GetConnectionAsync_EmptyFolder_AutoIncrementsEveryKeyButTheActiveState(string table, bool autoIncrements)
    {
        var connection = await _database.Context.GetConnectionAsync();

        var sql = await connection.ExecuteScalarAsync<string>(string.Format(TableSqlFormat, table));

        Assert.Equal(autoIncrements, sql.Contains(AutoIncrement, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("PRAGMA journal_mode", "delete")]
    [InlineData("PRAGMA foreign_keys", "1")]
    [InlineData("PRAGMA encoding", "UTF-8")]
    public async Task GetConnectionAsync_EmptyFolder_UsesDefaultPragmas(string pragma, string expected)
    {
        var connection = await _database.Context.GetConnectionAsync();

        Assert.Equal(expected, await connection.ExecuteScalarAsync<string>(pragma));
    }

    [Fact]
    public async Task GetConnectionAsync_CalledConcurrently_SharesOneConnection()
    {
        var calls = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(_database.Context.GetConnectionAsync)));

        Assert.All(calls, connection => Assert.Same(calls[0], connection));
    }

    [Fact]
    public async Task GetConnectionAsync_ExistingFile_KeepsSchemaAndRows()
    {
        var first = await _database.Context.GetConnectionAsync();
        await first.InsertAsync(new Exercise { Name = nameof(Exercise) });
        await first.CloseAsync();

        var reopened = new DatabaseContext(_database.FileSystem);
        var connection = await reopened.GetConnectionAsync();

        Assert.Equal(nameof(Exercise), Assert.Single(await connection.Table<Exercise>().ToListAsync()).Name);
        Assert.Equal(DatabaseContext.Tables.Count, (await connection.QueryScalarsAsync<string>(TablesSql)).Count);
        await connection.CloseAsync();
    }

    [Fact]
    public async Task GetConnectionAsync_PreviousOpenFailed_OpensAgain()
    {
        var missingFolder = Path.Combine(_database.Folder, MissingFolderName);
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.AppDataDirectory.Returns(missingFolder);
        var context = new DatabaseContext(fileSystem);

        await Assert.ThrowsAsync<SQLiteException>(context.GetConnectionAsync);
        Directory.CreateDirectory(missingFolder);
        var connection = await context.GetConnectionAsync();

        Assert.Equal(0, await connection.Table<Exercise>().CountAsync());
        await connection.CloseAsync();
    }

    [Fact]
    public async Task OpenReadOnly_Write_IsRefused()
    {
        await _database.Context.GetConnectionAsync();

        using var readOnly = DatabaseContext.OpenReadOnly(_database.FilePath);

        Assert.Throws<SQLiteException>(() => readOnly.Insert(new Exercise { Name = nameof(Exercise) }));
        Assert.Equal(0, readOnly.Table<Exercise>().Count());
    }

    private static string Describe(TableColumn column) =>
        column.IsKey ? $"{column.Name} {column.Type.ToLowerInvariant()} pk" : $"{column.Name} {column.Type.ToLowerInvariant()}";

    private sealed class TableColumn
    {
        public string Name { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public bool IsKey { get; set; }
    }
}
