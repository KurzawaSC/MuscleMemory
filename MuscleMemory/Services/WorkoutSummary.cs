namespace MuscleMemory.Services;

public sealed record WorkoutSummary(double TotalVolume, IReadOnlyList<CompletedExerciseSummary> Exercises)
{
    public bool HasLoggedSets => Exercises.Any(exercise => exercise.Sets.Count > 0);

    public int NewRecordCount => Exercises.Sum(exercise => exercise.NewRecordCount);
}
