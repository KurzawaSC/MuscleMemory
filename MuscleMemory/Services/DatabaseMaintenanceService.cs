using MuscleMemory.Constants;
using MuscleMemory.Data;
using MuscleMemory.Data.Repositories;

namespace MuscleMemory.Services;

public sealed class DatabaseMaintenanceService(
    DatabaseContext context,
    IExerciseRepository exerciseRepository,
    IWorkoutRepository workoutRepository,
    IWorkoutSessionRepository sessionRepository,
    ISessionExerciseRepository sessionExerciseRepository,
    IWorkoutSetRepository setRepository,
    IActiveWorkoutStateRepository activeWorkoutStateRepository,
    IDataChangeNotifier dataChanges) : IDatabaseMaintenanceService
{
    private const string VacuumIntoSql = "VACUUM INTO ?";

    private static string ExportSnapshotPath => Path.Combine(FileSystem.CacheDirectory, DatabaseNames.ExportFileName);

    public async Task<string?> CreateExportSnapshotAsync()
    {
        if (!File.Exists(context.DatabasePath))
        {
            return null;
        }

        var snapshotPath = ExportSnapshotPath;
        File.Delete(snapshotPath);

        var connection = await context.GetConnectionAsync();
        await connection.ExecuteAsync(VacuumIntoSql, snapshotPath);
        return snapshotPath;
    }

    public Task DeleteExportSnapshotAsync()
    {
        var snapshotPath = ExportSnapshotPath;
        return Task.Run(() => File.Delete(snapshotPath));
    }

    public async Task ClearAllDataAsync()
    {
        var connection = await context.GetConnectionAsync();
        await connection.RunInTransactionAsync(transaction =>
        {
            workoutRepository.Clear(transaction);
            exerciseRepository.Clear(transaction);
            setRepository.Clear(transaction);
            sessionExerciseRepository.Clear(transaction);
            sessionRepository.Clear(transaction);
            activeWorkoutStateRepository.Clear(transaction);
        });
        dataChanges.Notify(DataArea.All);
    }
}
