using MuscleMemory.Models;

namespace MuscleMemory.Services;

public sealed record WorkoutHistoryExercise(int SessionExerciseId, string ExerciseName, IReadOnlyList<WorkoutSet> Sets);
