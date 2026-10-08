using MuscleMemory.Services;

namespace MuscleMemory.ViewModels;

public sealed record WorkoutHistoryExerciseItem(
    WorkoutHistoryExercise Exercise,
    IReadOnlyList<LoggedSetItem> Sets)
{
    public static WorkoutHistoryExerciseItem Create(WorkoutHistoryExercise exercise) =>
        new(exercise, [.. exercise.Sets.Select(LoggedSetItem.Create)]);
}
