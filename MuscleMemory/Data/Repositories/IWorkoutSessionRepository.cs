using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public interface IWorkoutSessionRepository
{
    Task<WorkoutSession> CreateAsync(Workout workout);
    Task FinishAsync(int sessionId);
    Task<WorkoutSession?> GetAsync(int sessionId);
    Task<List<WorkoutSession>> GetCompletedByIdsAsync(IReadOnlyCollection<int> sessionIds);
    Task<List<WorkoutSession>> GetCompletedForWorkoutAsync(int workoutId);
    Task ClearAsync();
}
