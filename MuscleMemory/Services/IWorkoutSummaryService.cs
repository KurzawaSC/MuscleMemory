using MuscleMemory.Models;

namespace MuscleMemory.Services;

public interface IWorkoutSummaryService
{
    Task<WorkoutSummary> BuildAsync(IReadOnlyList<SessionExercise> performedExercises);
}
