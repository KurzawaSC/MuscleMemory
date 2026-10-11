using System.Globalization;
using MuscleMemory.Extensions;
using MuscleMemory.Localization;
using MuscleMemory.Resources.Strings;
using MuscleMemory.Services;

namespace MuscleMemory.ViewModels;

public sealed record ExerciseHistoryItem(
    ExerciseHistoryEntry Entry,
    string DateText,
    string SetCountText,
    string VolumeText,
    IReadOnlyList<LoggedSetItem> Sets)
{
    public static ExerciseHistoryItem Create(ExerciseHistoryEntry entry) =>
        new(
            entry,
            entry.LocalDate.ToString(AppResources.SessionDateTimeFormat, CultureInfo.CurrentCulture),
            Plurals.Format(nameof(AppResources.SetCount_Other), entry.Sets.Count),
            string.Format(CultureInfo.CurrentCulture, AppResources.VolumeFormat, entry.Sets.TotalVolume()),
            [.. entry.Sets.Select(LoggedSetItem.Create)]);
}
