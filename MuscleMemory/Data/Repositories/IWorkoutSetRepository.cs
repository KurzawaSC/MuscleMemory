using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public interface IWorkoutSetRepository
{
    Task AddAsync(WorkoutSet set);
    Task UpdateAsync(WorkoutSet set);
    Task DeleteAsync(int setId);
    Task DeleteForLoggedExerciseAsync(int workoutExerciseId, int workoutSessionId);
    Task<List<WorkoutSet>> GetForWorkoutExerciseAsync(int workoutExerciseId, int workoutSessionId);
    Task<List<WorkoutSet>> GetAllForWorkoutExerciseAsync(int workoutExerciseId);
    Task<List<WorkoutSet>> GetForSessionAsync(int workoutSessionId);
    Task<List<WorkoutSet>> GetLastSessionSetsAsync(int workoutExerciseId, int currentSessionId);
    Task ClearAsync();
}
