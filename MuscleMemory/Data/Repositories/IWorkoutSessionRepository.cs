using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public interface IWorkoutSessionRepository
{
    Task<WorkoutSession> CreateAsync(Workout workout);
    Task FinishAsync(int sessionId);
    Task<WorkoutSession?> GetAsync(int sessionId);
    Task<List<WorkoutSession>> GetByIdsAsync(IReadOnlyCollection<int> sessionIds);
    Task<List<WorkoutSession>> GetForWorkoutAsync(int workoutId);
    Task ClearAsync();
}
