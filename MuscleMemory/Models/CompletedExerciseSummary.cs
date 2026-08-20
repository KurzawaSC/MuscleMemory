namespace MuscleMemory.Models;

public sealed record CompletedExerciseSummary(string ExerciseName, IReadOnlyList<WorkoutSet> Sets);
