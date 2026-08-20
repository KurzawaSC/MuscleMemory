using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public interface IWorkoutSetRepository
{
    Task AddAsync(WorkoutSet set);
    Task UpdateAsync(WorkoutSet set);
    Task DeleteAsync(int setId);
    Task DeleteForSessionExerciseAsync(int sessionExerciseId);
    Task<List<WorkoutSet>> GetForSessionExerciseAsync(int sessionExerciseId);
    Task<List<WorkoutSet>> GetForSessionExercisesAsync(IReadOnlyCollection<int> sessionExerciseIds);
    Task<List<WorkoutSet>> GetLastSessionSetsAsync(int exerciseId, int currentSessionId);
    Task ClearAsync();
}
