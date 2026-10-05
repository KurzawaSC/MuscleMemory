using MuscleMemory.Models;

namespace MuscleMemory.Services;

public sealed record WorkoutSummary(double TotalVolume, IReadOnlyList<CompletedExerciseSummary> Exercises)
{
    public bool HasLoggedSets => Exercises.Count > 0;
}
