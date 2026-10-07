namespace MuscleMemory.Services;

public interface IHistoryQueryService
{
    Task<IReadOnlyList<ExerciseHistoryEntry>> GetExerciseHistoryAsync(int exerciseId);
    Task<IReadOnlyList<WorkoutHistorySession>> GetWorkoutHistoryAsync(int workoutId);
    Task<WorkoutHistorySession?> GetWorkoutHistorySessionAsync(int sessionId);
}
