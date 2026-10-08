using MuscleMemory.Data.Repositories;
using MuscleMemory.Models;

namespace MuscleMemory.Services;

public interface IActiveWorkoutSessionService
{
    Task<StartedSession> StartAsync(Workout workout);
    Task<ResumableSession?> FindResumableAsync();
    Task<List<SessionExercise>> GetExercisesAsync(int sessionId);
    Task SaveStateAsync(ActiveWorkoutState state);
    Task FinishAsync(int sessionId);
    Task<List<WorkoutSet>> GetSetsAsync(int sessionExerciseId);
    Task<List<WorkoutSet>> GetLastSessionSetsAsync(int exerciseId, int currentSessionId);
    Task AddSetAsync(WorkoutSet set);
    Task UpdateSetAsync(int setId, double weight, int reps);
    Task DeleteSetAsync(int setId);
    Task<bool> IsPlanCompleteAsync(IReadOnlyCollection<SessionExercise> exercises);
}
