using System.Globalization;
using MuscleMemory.Constants;
using MuscleMemory.Extensions;
using MuscleMemory.Services;

namespace MuscleMemory.ViewModels;

public sealed record WorkoutHistoryItem(
    WorkoutHistorySession Session,
    string ChipText,
    string DateText,
    string StatsText,
    string VolumeText,
    IReadOnlyList<WorkoutHistoryExerciseItem> Exercises)
{
    public static WorkoutHistoryItem Create(WorkoutHistorySession session, string durationText, bool sharesDate)
    {
        var setCount = session.Exercises.Sum(exercise => exercise.Sets.Count);

        return new WorkoutHistoryItem(
            session,
            session.LocalStartTime.ToString(sharesDate ? UiText.ShortDateTimeFormat : UiText.ShortDateFormat, CultureInfo.InvariantCulture),
            session.LocalStartTime.ToString(UiText.LongDateFormat, CultureInfo.InvariantCulture),
            string.Format(CultureInfo.CurrentCulture, UiText.SessionStatsFormat, durationText, setCount, CountCaption.Sets(setCount)),
            string.Format(CultureInfo.CurrentCulture, UiText.VolumeNumberFormat, session.TotalVolume),
            [.. session.Exercises.Select(WorkoutHistoryExerciseItem.Create)]);
    }
}
