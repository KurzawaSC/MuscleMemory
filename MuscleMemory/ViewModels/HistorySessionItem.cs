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
    public static HistorySessionItem Create(WorkoutHistorySession session, string durationText)
    {
        var setCount = session.Exercises.Sum(exercise => exercise.Sets.Count);

        return new HistorySessionItem(
            session,
            session.LocalStartTime.ToString(UiText.ShortDateFormat, CultureInfo.InvariantCulture),
            session.LocalStartTime.ToString(UiText.LongDateFormat, CultureInfo.InvariantCulture),
            string.Format(UiText.SessionStatsFormat, durationText, setCount, setCount == 1 ? UiText.CaptionSet : UiText.CaptionSets),
            string.Format(CultureInfo.InvariantCulture, UiText.VolumeNumberFormat, session.TotalVolume));
    }
}
