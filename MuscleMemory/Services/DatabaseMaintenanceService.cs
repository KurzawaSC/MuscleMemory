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
    public string DatabaseFilePath => context.DatabasePath;

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
