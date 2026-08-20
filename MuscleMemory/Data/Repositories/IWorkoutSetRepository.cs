using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public interface IWorkoutSetRepository
{
    Task AddAsync(WorkoutSet set);
    Task UpdateAsync(WorkoutSet set);
    Task DeleteAsync(int setId);
    Task DeleteForLoggedExerciseAsync(int workoutExerciseId, int workoutSessionId);
    Task<List<WorkoutSet>> GetForWorkoutExerciseAsync(int workoutExerciseId, int workoutSessionId);
    Task<List<WorkoutSet>> GetForWorkoutExercisesAsync(IReadOnlyCollection<int> workoutExerciseIds);
    Task<List<WorkoutSet>> GetForSessionsAsync(IReadOnlyCollection<int> workoutSessionIds);
    Task<List<WorkoutSet>> GetLastSessionSetsAsync(int workoutExerciseId, int currentSessionId);
    Task ClearAsync();
}
