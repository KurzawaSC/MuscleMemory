using System.Globalization;
using MuscleMemory.Constants;
using MuscleMemory.Services;

namespace MuscleMemory.ViewModels;

public sealed record HistorySessionItem(
    WorkoutHistorySession Session,
    string ChipText,
    string DateText,
    string StatsText,
    string VolumeText)
{
    public static HistorySessionItem Create(WorkoutHistorySession session, string durationText, bool sharesDate)
    {
        var setCount = session.Exercises.Sum(exercise => exercise.Sets.Count);

        return new HistorySessionItem(
            session,
            session.LocalStartTime.ToString(sharesDate ? UiText.ShortDateTimeFormat : UiText.ShortDateFormat, CultureInfo.InvariantCulture),
            session.LocalStartTime.ToString(UiText.LongDateFormat, CultureInfo.InvariantCulture),
            string.Format(UiText.SessionStatsFormat, durationText, setCount, setCount == 1 ? UiText.CaptionSet : UiText.CaptionSets),
            string.Format(CultureInfo.CurrentCulture, UiText.VolumeNumberFormat, session.TotalVolume));
    }
}
