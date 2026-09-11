using MuscleMemory.Constants;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

public sealed record WorkoutListItem(Workout Workout, string ExerciseSummary, int TotalSets, bool IsFeatured)
{
    public string SetsText => string.Format(UiText.CountFormat, TotalSets, TotalSets == 1 ? UiText.CaptionSet : UiText.CaptionSets);

    public static WorkoutListItem Create(Workout workout, IEnumerable<WorkoutExercise> exercises, bool isFeatured)
    {
        var ordered = exercises.OrderBy(exercise => exercise.Order).ToList();

        return new WorkoutListItem(
            workout,
            string.Join(UiText.ListSeparator, ordered.Select(exercise => exercise.ExerciseName)),
            ordered.Sum(exercise => exercise.Sets),
            isFeatured);
    }
}
