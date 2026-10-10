using MuscleMemory.Localization;
using MuscleMemory.Models;
using MuscleMemory.Resources.Strings;

namespace MuscleMemory.ViewModels;

public sealed record WorkoutListItem(Workout Workout, string ExerciseSummary, int TotalSets, bool HasExercises)
{
    public string SetsText => Plurals.Format(nameof(AppResources.SetCount_Other), TotalSets);

    public static WorkoutListItem Create(Workout workout, IEnumerable<WorkoutExercise> exercises)
    {
        var ordered = exercises.OrderBy(exercise => exercise.Order).ToList();

        return new WorkoutListItem(
            workout,
            string.Join(AppResources.ListSeparator, ordered.Select(exercise => exercise.ExerciseName)),
            ordered.Sum(exercise => exercise.Sets),
            ordered.Count > 0);
    }
}
