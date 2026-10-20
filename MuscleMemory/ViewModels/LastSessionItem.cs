using System.Globalization;
using MuscleMemory.Localization;
using MuscleMemory.Models;
using MuscleMemory.Resources.Strings;

namespace MuscleMemory.ViewModels;

public sealed record LastSessionItem(string Text, string Description)
{
    public static LastSessionItem Create(IReadOnlyList<WorkoutSet> sets)
    {
        if (sets.Count == 0)
        {
            return new LastSessionItem(AppResources.FirstTimePerformingExercise, AppResources.FirstTimeDescription);
        }

        var results = sets.Select(set => string.Format(CultureInfo.CurrentCulture, AppResources.SetResultFormat, set.Weight, set.Reps));
        var spoken = sets.Select(set => string.Format(
            CultureInfo.CurrentCulture,
            AppResources.LastSessionSetDescriptionFormat,
            string.Format(CultureInfo.CurrentCulture, AppResources.WeightFormat, set.Weight),
            Plurals.Format(nameof(AppResources.RepCount_Other), set.Reps)));

        return new LastSessionItem(
            AppResources.LastSessionPrefix + string.Join(AppResources.ResultSeparator, results),
            string.Format(CultureInfo.CurrentCulture, AppResources.LastSessionDescriptionFormat, string.Join(AppResources.ResultSeparator, spoken)));
    }
}
