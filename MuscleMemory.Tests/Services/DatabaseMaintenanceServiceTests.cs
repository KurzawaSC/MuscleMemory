using MuscleMemory.Constants;
using MuscleMemory.Models;
using MuscleMemory.Tests.Testing;

namespace MuscleMemory.Tests.Services;

public sealed class DatabaseMaintenanceServiceTests : IAsyncLifetime
{
    private const string QuickCheckPassed = "ok";
    private const string StaleContent = "stale";
    private const int SeededSets = 4;

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

    private static async Task SeedAsync(TestApp app)
    {
        var bench = await app.Data.ExerciseAsync("Bench Press", MuscleGroup.Chest, EquipmentType.Barbell);
        var press = await app.Data.ExerciseAsync("Overhead Press", MuscleGroup.Shoulders, EquipmentType.Barbell);
        var squat = await app.Data.ExerciseAsync("Back Squat", MuscleGroup.Legs, EquipmentType.Barbell);
        var push = await app.Data.WorkoutAsync("Push Day", bench, press);
        await app.Data.WorkoutAsync("Leg Day", squat);
        await app.Data.CompletedSessionAsync(push, TestApp.StartUtc, new LoggedSet(0, 80, 5), new LoggedSet(0, 82.5, 5), new LoggedSet(1, 42.5, 8));
        await app.Data.CompletedSessionAsync(push, TestApp.StartUtc + OneDay, new LoggedSet(0, 85, 3));
    }
}
