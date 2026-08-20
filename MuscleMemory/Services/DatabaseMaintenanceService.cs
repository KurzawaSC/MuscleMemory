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
        await workoutRepository.ClearAsync();
        await exerciseRepository.ClearAsync();
        await setRepository.ClearAsync();
        await sessionExerciseRepository.ClearAsync();
        await sessionRepository.ClearAsync();
        await activeWorkoutStateRepository.ClearAsync();
    }
}
