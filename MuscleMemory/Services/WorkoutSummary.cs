using MuscleMemory.Models;

namespace MuscleMemory.Services;

public sealed record WorkoutSummary(double TotalVolume, IReadOnlyList<CompletedExerciseSummary> Exercises)
{
    public bool HasLoggedSets => Exercises.Any(exercise => exercise.Sets.Count > 0);
}
