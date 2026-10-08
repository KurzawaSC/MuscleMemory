using System.Globalization;
using MuscleMemory.Constants;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

public sealed record LoggedSetItem(
    WorkoutSet Set,
    string SetNumberText,
    string WeightText,
    string RepsText)
{
    public static LoggedSetItem Create(WorkoutSet set) =>
        new(
            set,
            set.SetNumber.ToString(CultureInfo.CurrentCulture),
            string.Format(CultureInfo.CurrentCulture, UiText.WeightFormat, set.Weight),
            set.Reps.ToString(CultureInfo.CurrentCulture));
}
