using System.Globalization;
using MuscleMemory.Constants;
using MuscleMemory.Extensions;
using MuscleMemory.Services;

namespace MuscleMemory.ViewModels;

public sealed record ExerciseHistoryItem(
    ExerciseHistoryEntry Entry,
    string DateText,
    string SetCountText,
    string VolumeText)
{
    public static ExerciseHistoryItem Create(ExerciseHistoryEntry entry)
    {
        var setCount = entry.Sets.Count;

        return new ExerciseHistoryItem(
            entry,
            entry.LocalDate.ToString(UiText.SessionDateTimeFormat, CultureInfo.InvariantCulture),
            string.Format(UiText.CountFormat, setCount, CountCaption.Sets(setCount)),
            string.Format(CultureInfo.CurrentCulture, UiText.VolumeFormat, entry.Sets.TotalVolume()));
    }
}
