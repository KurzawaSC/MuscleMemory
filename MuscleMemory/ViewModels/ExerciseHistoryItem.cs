using System.Globalization;
using MuscleMemory.Extensions;
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
    public static ExerciseHistoryItem Create(ExerciseHistoryEntry entry)
    {
        var setCount = entry.Sets.Count;

        return new ExerciseHistoryItem(
            entry,
            entry.LocalDate.ToString(AppResources.SessionDateTimeFormat, CultureInfo.InvariantCulture),
            string.Format(CultureInfo.CurrentCulture, AppResources.CountFormat, setCount, CountCaption.Sets(setCount)),
            string.Format(CultureInfo.CurrentCulture, AppResources.VolumeFormat, entry.Sets.TotalVolume()),
            [.. entry.Sets.Select(LoggedSetItem.Create)]);
    }
}
