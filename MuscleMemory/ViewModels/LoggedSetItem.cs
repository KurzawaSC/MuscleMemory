using System.Globalization;
using MuscleMemory.Localization;
using MuscleMemory.Models;
using MuscleMemory.Resources.Strings;

namespace MuscleMemory.ViewModels;

public sealed record LoggedSetItem(
    WorkoutSet Set,
    string SetNumberText,
    string WeightText,
    string RepsText,
    string Description,
    bool IsNewRecord)
{
    public static LoggedSetItem Create(WorkoutSet set) => Create(set, isNewRecord: false);

    public static LoggedSetItem Create(WorkoutSet set, bool isNewRecord)
    {
        var weightText = string.Format(CultureInfo.CurrentCulture, AppResources.WeightFormat, set.Weight);
        var description = string.Format(CultureInfo.CurrentCulture, AppResources.SetDescriptionFormat, set.SetNumber, weightText, Plurals.Format(nameof(AppResources.RepCount_Other), set.Reps));

        return new LoggedSetItem(
            set,
            set.SetNumber.ToString(CultureInfo.CurrentCulture),
            weightText,
            set.Reps.ToString(CultureInfo.CurrentCulture),
            isNewRecord ? string.Format(CultureInfo.CurrentCulture, AppResources.NewRecordSetDescriptionFormat, description) : description,
            isNewRecord);
    }
}
