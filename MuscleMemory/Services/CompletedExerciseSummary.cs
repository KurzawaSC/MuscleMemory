using MuscleMemory.Models;

namespace MuscleMemory.Services;

public sealed record CompletedExerciseSummary(string ExerciseName, IReadOnlyList<WorkoutSet> Sets);
