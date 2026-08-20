using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public interface IWorkoutSessionRepository
{
    Task<int> CreateAsync(int workoutId);
    Task FinishAsync(int sessionId);
    Task<WorkoutSession?> GetAsync(int sessionId);
    Task<List<WorkoutSession>> GetForWorkoutAsync(int workoutId);
    Task ClearAsync();
}
