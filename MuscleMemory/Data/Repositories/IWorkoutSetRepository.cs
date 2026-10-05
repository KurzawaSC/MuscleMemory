using SQLite;
using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public interface IWorkoutSetRepository
{
    Task AddAsync(WorkoutSet set);
    Task UpdateAsync(int setId, double weight, int reps);
    Task DeleteAsync(int setId);
    Task<List<WorkoutSet>> GetForSessionExerciseAsync(int sessionExerciseId);
    Task<List<WorkoutSet>> GetForSessionExercisesAsync(IReadOnlyCollection<int> sessionExerciseIds);
    Task<List<WorkoutSet>> GetLastSessionSetsAsync(int exerciseId, int currentSessionId);
    void Clear(SQLiteConnection transaction);
}
