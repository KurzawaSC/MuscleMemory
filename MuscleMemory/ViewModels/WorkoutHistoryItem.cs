using System.Globalization;
using MuscleMemory.Constants;
using MuscleMemory.Extensions;
using MuscleMemory.Resources.Strings;
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
            session.LocalStartTime.ToString(sharesDate ? AppResources.ShortDateTimeFormat : AppResources.ShortDateFormat, CultureInfo.InvariantCulture),
            session.LocalStartTime.ToString(AppResources.LongDateFormat, CultureInfo.InvariantCulture),
            string.Format(CultureInfo.CurrentCulture, AppResources.SessionStatsFormat, durationText, setCount, CountCaption.Sets(setCount)),
            string.Format(CultureInfo.CurrentCulture, DisplayFormats.VolumeNumber, session.TotalVolume),
            [.. session.Exercises.Select(WorkoutHistoryExerciseItem.Create)]);
    }
}
