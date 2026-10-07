using System.Globalization;
using MuscleMemory.Constants;
using MuscleMemory.Extensions;
using MuscleMemory.Services;

namespace MuscleMemory.ViewModels;

public sealed record SummaryExerciseItem(string ExerciseName, string VolumeText, IReadOnlyList<SummarySetBar> Sets)
{
    public static SummaryExerciseItem Create(CompletedExerciseSummary exercise)
    {
        var volumes = exercise.Sets.Select(set => set.Volume()).ToList();
        var bestVolume = volumes.DefaultIfEmpty().Max();
        var hasSingleBest = volumes.Count > 1 && volumes.Count(volume => volume == bestVolume) == 1;

        return new SummaryExerciseItem(
            exercise.ExerciseName,
            string.Format(CultureInfo.CurrentCulture, UiText.VolumeFormat, volumes.Sum()),
            [.. exercise.Sets.Select(set => new SummarySetBar(
                set.SetNumber,
                string.Format(CultureInfo.CurrentCulture, UiText.LoggedSetFormat, set.Weight, set.Reps),
                new Rect(0, 0, bestVolume > 0 ? set.Volume() / bestVolume : 0, 1),
                hasSingleBest && set.Volume() == bestVolume))]);
    }
}
