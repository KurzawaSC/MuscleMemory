using MuscleMemory.Models;

namespace MuscleMemory.Services;

public sealed record ExerciseHistoryEntry(DateTime Date, string WorkoutName, IReadOnlyList<WorkoutSet> Sets);
