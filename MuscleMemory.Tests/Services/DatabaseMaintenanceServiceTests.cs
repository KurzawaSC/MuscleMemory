using MuscleMemory.Constants;
using MuscleMemory.Data;
using MuscleMemory.Models;
using MuscleMemory.Services;
using MuscleMemory.Tests.Testing;
using NSubstitute;
using SQLite;

namespace MuscleMemory.Tests.Services;

public sealed class DatabaseMaintenanceServiceTests : IAsyncLifetime
{
    private const string QuickCheckPassed = "ok";
    private const string StaleContent = "stale";
    private const int SeededSets = 4;
    private const int SeededExercises = 3;
    private const int SeededWorkouts = 2;
    private const int SeededSessions = 2;
    private const string PickedFileName = "backup.db3";
    private const string PickerFolderName = "picker-copy";
    private const string NeighbourFileName = "other.txt";
    private const string MainSchema = "main";
    private const string AttachedSchemasSql = "SELECT name FROM pragma_database_list";

    private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

    private readonly TestApp _app = new();

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _app.DisposeAsync();

    private string ExportPath => Path.Combine(_app.Database.CacheFolder, DatabaseNames.ExportFileName);

    private string ImportPath => Path.Combine(_app.Database.CacheFolder, DatabaseNames.ImportFileName);

    [Fact]
    public async Task CreateExportSnapshotAsync_NoDatabaseFile_ReturnsNull()
    {
        Assert.Null(await _app.Maintenance.CreateExportSnapshotAsync());
        Assert.False(File.Exists(ExportPath));
    }

    [Fact]
    public async Task CreateExportSnapshotAsync_Data_WritesSnapshotIntoTheCache()
    {
        await SeedAsync(_app);

        Assert.Equal(ExportPath, await _app.Maintenance.CreateExportSnapshotAsync());
        Assert.True(File.Exists(ExportPath));
    }

    [Fact]
    public async Task CreateExportSnapshotAsync_Data_PassesQuickCheckAndHoldsEveryRow()
    {
        await SeedAsync(_app);

        var snapshotPath = await _app.Maintenance.CreateExportSnapshotAsync();

        Assert.NotNull(snapshotPath);
        Assert.Equal(QuickCheckPassed, DatabaseRows.QuickCheck(snapshotPath));
        var exported = DatabaseRows.ReadAll(snapshotPath);
        Assert.Equal(DatabaseRows.ReadAll(_app.Database.FilePath), exported);
        Assert.Equal(SeededSets, exported[nameof(WorkoutSet)].Count);
    }

    [Fact]
    public async Task CreateExportSnapshotAsync_PreviousSnapshotLeft_ReplacesIt()
    {
        await SeedAsync(_app);
        await File.WriteAllTextAsync(ExportPath, StaleContent, TestContext.Current.CancellationToken);

        var snapshotPath = await _app.Maintenance.CreateExportSnapshotAsync();

        Assert.NotNull(snapshotPath);
        Assert.Equal(QuickCheckPassed, DatabaseRows.QuickCheck(snapshotPath));
        Assert.Equal(DatabaseRows.ReadAll(_app.Database.FilePath), DatabaseRows.ReadAll(snapshotPath));
    }

    [Fact]
    public async Task DeleteTemporaryFilesAsync_SnapshotAndImportCopyLeft_DeletesBoth()
    {
        await SeedAsync(_app);
        await _app.Maintenance.CreateExportSnapshotAsync();
        await File.WriteAllTextAsync(ImportPath, StaleContent, TestContext.Current.CancellationToken);

        await _app.Maintenance.DeleteTemporaryFilesAsync();

        Assert.False(File.Exists(ExportPath));
        Assert.False(File.Exists(ImportPath));
        Assert.True(File.Exists(_app.Database.FilePath));
    }

    [Fact]
    public async Task DeleteTemporaryFilesAsync_NothingLeft_Succeeds()
    {
        await _app.Maintenance.DeleteTemporaryFilesAsync();

        Assert.Empty(Directory.EnumerateFileSystemEntries(_app.Database.CacheFolder));
    }

    [Fact]
    public async Task PrepareImportAsync_ValidExport_CopiesItAndCountsWhatItHolds()
    {
        await using var source = new TestApp();
        var backupPath = await ExportSeededAsync(source);

        var preview = await PrepareAsync(backupPath);

        Assert.Equal(new ImportPreview(ImportFileStatus.Valid, new ImportCounts(SeededExercises, SeededWorkouts, SeededSessions)), preview);
        Assert.Equal(await File.ReadAllBytesAsync(backupPath, TestContext.Current.CancellationToken), await File.ReadAllBytesAsync(ImportPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ImportAsync_ValidExport_ReplacesEveryTableWithTheBackup()
    {
        await SeedOwnDataAsync();
        await using var source = new TestApp();
        var backupPath = await ExportSeededAsync(source);
        await PrepareAsync(backupPath);

        await _app.Maintenance.ImportAsync();

        Assert.Equal(DatabaseRows.Read(backupPath, ImportedTables), DatabaseRows.Read(_app.Database.FilePath, ImportedTables));
        Assert.Equal(SeededSets, await _app.Database.CountAsync(nameof(WorkoutSet)));
    }

    [Fact]
    public async Task ImportAsync_ValidExport_NotifiesEveryAreaOnce()
    {
        await using var source = new TestApp();
        await PrepareAsync(await ExportSeededAsync(source));
        _app.DataChanges.ClearReceivedCalls();

        await _app.Maintenance.ImportAsync();

        _app.DataChanges.Received(1).Notify(DataArea.All);
        _app.DataChanges.ReceivedWithAnyArgs(1).Notify(default);
    }

    [Fact]
    public async Task ImportAsync_BackupTakenDuringAWorkout_NeverImportsTheActiveState()
    {
        var ownSession = await _app.Data.StartAsync(await _app.Data.WorkoutAsync("Mine"));
        await _app.Data.SaveActiveStateAsync(ownSession);
        await using var source = new TestApp();
        var push = await SeedAsync(source);
        var open = await source.Data.StartAsync(push);
        await source.Data.LogSetAsync(open.Exercises[0], 90, 2);
        await source.Data.SaveActiveStateAsync(open);
        var backupPath = await source.Maintenance.CreateExportSnapshotAsync();
        Assert.NotNull(backupPath);
        var preview = await PrepareAsync(backupPath);

        await _app.Maintenance.ImportAsync();

        Assert.Equal(SeededSessions, preview.Counts.Sessions);
        Assert.Equal(0, await _app.Database.CountAsync(nameof(ActiveWorkoutState)));
        Assert.Null(await _app.ActiveSessions.FindResumableAsync());
        Assert.Equal(SeededSessions + 1, await _app.Database.CountAsync(nameof(WorkoutSession)));
        Assert.Equal(SeededSessions, (await _app.History.GetWorkoutHistoryAsync(push.Id)).Count);
    }

    [Fact]
    public async Task ImportAsync_ThenAddingData_ContinuesAfterTheImportedIds()
    {
        await using var source = new TestApp();
        await PrepareAsync(await ExportSeededAsync(source));
        await _app.Maintenance.ImportAsync();

        var added = await _app.Data.ExerciseAsync("Deadlift");

        Assert.Equal(SeededExercises + 1, added.Id);
        Assert.Equal(SeededExercises + 1, (await _app.Exercises.GetAllAsync()).Count);
    }

    [Fact]
    public async Task ImportAsync_CopyFails_KeepsCurrentDataAndDetachesTheBackup()
    {
        await SeedOwnDataAsync();
        var before = DatabaseRows.ReadAll(_app.Database.FilePath);
        await using var source = new TestApp();
        await PrepareAsync(await ExportSeededAsync(source));
        await _app.Database.FailOnAsync(nameof(Exercise), TableOperation.Insert);
        _app.DataChanges.ClearReceivedCalls();

        await Assert.ThrowsAsync<SQLiteException>(_app.Maintenance.ImportAsync);

        Assert.Equal(before, DatabaseRows.ReadAll(_app.Database.FilePath));
        Assert.Equal([MainSchema], await (await _app.Database.ConnectionAsync()).QueryScalarsAsync<string>(AttachedSchemasSql));
        _app.DataChanges.DidNotReceiveWithAnyArgs().Notify(default);
    }

    [Fact]
    public async Task PrepareImportAsync_PlainTextFile_IsNotABackup() =>
        Assert.Equal(ImportPreview.Rejected(ImportFileStatus.NotBackup), await PrepareAsync(Write(ImportFiles.PlainText)));

    [Fact]
    public async Task PrepareImportAsync_EmptyFile_IsNotABackup() =>
        Assert.Equal(ImportPreview.Rejected(ImportFileStatus.NotBackup), await PrepareAsync(Write(ImportFiles.Empty)));

    [Fact]
    public async Task PrepareImportAsync_UnrelatedDatabase_IsNotABackup() =>
        Assert.Equal(ImportPreview.Rejected(ImportFileStatus.NotBackup), await PrepareAsync(Write(ImportFiles.UnrelatedDatabase)));

    [Fact]
    public async Task PrepareImportAsync_DamagedPages_IsDamaged()
    {
        await using var source = new TestApp();
        var backupPath = await ExportSeededAsync(source);
        ImportFiles.Corrupt(backupPath);

        Assert.Equal(ImportPreview.Rejected(ImportFileStatus.Damaged), await PrepareAsync(backupPath));
    }

    [Theory]
    [MemberData(nameof(TableNames))]
    public async Task PrepareImportAsync_TableMissing_IsIncompatible(string table) =>
        Assert.Equal(
            ImportPreview.Rejected(ImportFileStatus.Incompatible),
            await PrepareAsync(Write(path => ImportFiles.WithoutTable(path, DatabaseContext.Tables.Single(type => type.Name == table)))));

    [Fact]
    public async Task PrepareImportAsync_ColumnMissing_IsIncompatible() =>
        Assert.Equal(ImportPreview.Rejected(ImportFileStatus.Incompatible), await PrepareAsync(Write(ImportFiles.WithoutRepsColumn)));

    [Fact]
    public async Task PrepareImportAsync_ExtraTableAndColumn_IsValid() =>
        Assert.Equal(new ImportPreview(ImportFileStatus.Valid, ImportCounts.None), await PrepareAsync(Write(ImportFiles.WithExtraTableAndColumn)));

    [Fact]
    public async Task PrepareImportAsync_RejectedFile_LeavesCurrentDataUntouched()
    {
        await SeedOwnDataAsync();
        var before = DatabaseRows.ReadAll(_app.Database.FilePath);

        await PrepareAsync(Write(ImportFiles.WithoutRepsColumn));

        Assert.Equal(before, DatabaseRows.ReadAll(_app.Database.FilePath));
    }

    [Fact]
    public async Task DeleteImportCopiesAsync_PickerCopyInCacheFolder_DeletesBothCopiesAndTheEmptyFolder()
    {
        var pickedPath = PickerCopy(PickerFolderName);
        await PrepareAsync(pickedPath);

        await _app.Maintenance.DeleteImportCopiesAsync(pickedPath);

        Assert.False(File.Exists(ImportPath));
        Assert.False(File.Exists(pickedPath));
        Assert.False(Directory.Exists(Path.GetDirectoryName(pickedPath)));
        Assert.True(Directory.Exists(_app.Database.CacheFolder));
    }

    [Fact]
    public async Task DeleteImportCopiesAsync_PickerCopyBesideOtherFiles_KeepsTheFolder()
    {
        var pickedPath = PickerCopy(PickerFolderName);
        var neighbour = Path.Combine(_app.Database.CacheFolder, PickerFolderName, NeighbourFileName);
        await File.WriteAllTextAsync(neighbour, StaleContent, TestContext.Current.CancellationToken);
        await PrepareAsync(pickedPath);

        await _app.Maintenance.DeleteImportCopiesAsync(pickedPath);

        Assert.False(File.Exists(pickedPath));
        Assert.True(File.Exists(neighbour));
    }

    [Fact]
    public async Task DeleteImportCopiesAsync_PickedFileOutsideTheCache_KeepsIt()
    {
        var pickedPath = Write(ImportFiles.PlainText);
        await PrepareAsync(pickedPath);

        await _app.Maintenance.DeleteImportCopiesAsync(pickedPath);

        Assert.False(File.Exists(ImportPath));
        Assert.True(File.Exists(pickedPath));
    }

    [Fact]
    public async Task DeleteImportCopiesAsync_AfterAFailedImport_DeletesTheCopy()
    {
        await using var source = new TestApp();
        var pickedPath = PickerCopy(PickerFolderName);
        File.Copy(await ExportSeededAsync(source), pickedPath, overwrite: true);
        await PrepareAsync(pickedPath);
        await _app.Database.FailOnAsync(nameof(Exercise), TableOperation.Insert);
        await Assert.ThrowsAsync<SQLiteException>(_app.Maintenance.ImportAsync);

        await _app.Maintenance.DeleteImportCopiesAsync(pickedPath);

        Assert.Empty(Directory.EnumerateFileSystemEntries(_app.Database.CacheFolder));
    }

    public static TheoryData<string> TableNames => [.. DatabaseContext.Tables.Select(table => table.Name)];

    private static IEnumerable<Type> ImportedTables => DatabaseContext.Tables.Where(table => table != typeof(ActiveWorkoutState));

    private async Task<ImportPreview> PrepareAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        return await _app.Maintenance.PrepareImportAsync(stream);
    }

    private string Write(Action<string> create)
    {
        var path = Path.Combine(_app.Database.Folder, PickedFileName);
        create(path);
        return path;
    }

    private string PickerCopy(string folderName)
    {
        var folder = Path.Combine(_app.Database.CacheFolder, folderName);
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, PickedFileName);
        ImportFiles.PlainText(path);
        return path;
    }

    private async Task SeedOwnDataAsync()
    {
        var curl = await _app.Data.ExerciseAsync("Barbell Curl", MuscleGroup.Biceps);
        var arms = await _app.Data.WorkoutAsync("Arm Day", curl);
        await _app.Data.CompletedSessionAsync(arms, TestApp.StartUtc, new LoggedSet(0, 30, 12));
    }

    private static async Task<string> ExportSeededAsync(TestApp source)
    {
        await SeedAsync(source);
        var snapshotPath = await source.Maintenance.CreateExportSnapshotAsync();
        Assert.NotNull(snapshotPath);
        return snapshotPath;
    }

    private static async Task<Workout> SeedAsync(TestApp app)
    {
        var bench = await app.Data.ExerciseAsync("Bench Press", MuscleGroup.Chest, EquipmentType.Barbell);
        var press = await app.Data.ExerciseAsync("Overhead Press", MuscleGroup.Shoulders, EquipmentType.Barbell);
        var squat = await app.Data.ExerciseAsync("Back Squat", MuscleGroup.Legs, EquipmentType.Barbell);
        var push = await app.Data.WorkoutAsync("Push Day", bench, press);
        await app.Data.WorkoutAsync("Leg Day", squat);
        await app.Data.CompletedSessionAsync(push, TestApp.StartUtc, new LoggedSet(0, 80, 5), new LoggedSet(0, 82.5, 5), new LoggedSet(1, 42.5, 8));
        await app.Data.CompletedSessionAsync(push, TestApp.StartUtc + OneDay, new LoggedSet(0, 85, 3));
        return push;
    }
}
