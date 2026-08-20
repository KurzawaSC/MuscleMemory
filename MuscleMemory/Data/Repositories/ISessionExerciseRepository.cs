using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public interface ISessionExerciseRepository
{
    Task<List<SessionExercise>> CreateSnapshotAsync(int workoutSessionId, IReadOnlyList<WorkoutExercise> templateExercises);
    Task<List<SessionExercise>> GetForSessionAsync(int workoutSessionId);
    Task<List<SessionExercise>> GetForSessionsAsync(IReadOnlyCollection<int> workoutSessionIds);
    Task<List<SessionExercise>> GetForExerciseAsync(int exerciseId);
    Task<SessionExercise> AppendToSessionAsync(SessionExercise sessionExercise);
    Task DeleteAsync(int sessionExerciseId);
    Task ClearAsync();
}
