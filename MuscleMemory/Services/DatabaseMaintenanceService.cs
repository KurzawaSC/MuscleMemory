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
    IActiveWorkoutStateRepository activeWorkoutStateRepository) : IDatabaseMaintenanceService
{
    private const string VacuumIntoSql = "VACUUM INTO ?";

    public async Task<string?> CreateExportSnapshotAsync()
    {
        if (!File.Exists(context.DatabasePath))
        {
            return null;
        }

        var snapshotPath = Path.Combine(FileSystem.CacheDirectory, DatabaseNames.ExportFileName);
        File.Delete(snapshotPath);

        var connection = await context.GetConnectionAsync();
        await connection.ExecuteAsync(VacuumIntoSql, snapshotPath);
        return snapshotPath;
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
    }
}
