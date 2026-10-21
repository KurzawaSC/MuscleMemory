using MuscleMemory.Models;
using MuscleMemory.Progression;
using SQLite;

namespace MuscleMemory.Data.Repositories;

public interface IWorkoutSetRepository
{
    Task AddAsync(WorkoutSet set);
    Task UpdateAsync(int setId, double weight, int reps);
    Task DeleteAsync(int setId);
    Task<List<WorkoutSet>> GetForSessionExerciseAsync(int sessionExerciseId);
    Task<List<WorkoutSet>> GetForSessionExercisesAsync(IReadOnlyCollection<int> sessionExerciseIds);
    Task<List<WorkoutSet>> GetLastSessionSetsAsync(int exerciseId, int currentSessionId);
    Task<List<PerformedSet>> GetRecentSessionSetsAsync(int exerciseId, int sessionCount);
    Task<List<PerformedSet>> GetRepMaxesAsync(IReadOnlyCollection<int> exerciseIds);
    Task<List<SessionVolume>> GetSessionVolumesAsync(int exerciseId);
    Task<List<SessionRepTotals>> GetSessionRepTotalsAsync(int exerciseId);
    void Clear(SQLiteConnection transaction);
}
