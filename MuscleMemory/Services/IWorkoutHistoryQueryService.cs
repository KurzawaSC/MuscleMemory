namespace MuscleMemory.Services;

public interface IWorkoutHistoryQueryService
{
    Task<IReadOnlyList<ExerciseHistoryEntry>> GetExerciseHistoryAsync(int exerciseId);
    Task<IReadOnlyList<WorkoutHistorySession>> GetWorkoutHistoryAsync(int workoutId);
}
