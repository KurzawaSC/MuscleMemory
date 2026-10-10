using System.Globalization;
using MuscleMemory.Extensions;
using MuscleMemory.Models;
using MuscleMemory.Resources.Strings;

namespace MuscleMemory.ViewModels;

public sealed record LoggedSetItem(
    WorkoutSet Set,
    string SetNumberText,
    string WeightText,
    string RepsText,
    string Description)
{
    public static LoggedSetItem Create(WorkoutSet set)
    {
        var weightText = string.Format(CultureInfo.CurrentCulture, AppResources.WeightFormat, set.Weight);

        return new LoggedSetItem(
            set,
            set.SetNumber.ToString(CultureInfo.CurrentCulture),
            weightText,
            set.Reps.ToString(CultureInfo.CurrentCulture),
            string.Format(CultureInfo.CurrentCulture, AppResources.SetDescriptionFormat, set.SetNumber, weightText, set.Reps, CountCaption.Reps(set.Reps)));
    }
}
