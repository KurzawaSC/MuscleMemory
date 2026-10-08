using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using MuscleMemory.Constants;
using MuscleMemory.Extensions;
using MuscleMemory.Services;

namespace MuscleMemory.ViewModels;

public sealed partial class WorkoutSummaryViewModel : ObservableObject
{
    private readonly IWorkoutTimerService _timer;

    public WorkoutSummaryViewModel(IWorkoutTimerService timer)
    {
        _timer = timer;
        TotalTimeText = ZeroTimeText;
    }

    private string ZeroTimeText => _timer.FormatDuration(TimeSpan.Zero);

    [ObservableProperty]
    public partial bool IsVisible { get; private set; }

    [ObservableProperty]
    public partial string TotalTimeText { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial double TotalVolume { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalSetsCaption))]
    public partial int TotalSets { get; private set; }

    public string TotalSetsCaption => CountCaption.Sets(TotalSets);

    [ObservableProperty]
    public partial string DateText { get; private set; } = string.Empty;

    public ObservableCollection<SummaryExerciseItem> Exercises { get; } = [];

    public void Show(WorkoutSummary summary, DateTime startTimeUtc)
    {
        TotalTimeText = _timer.ElapsedSince(startTimeUtc);
        Exercises.ReplaceAll(summary.Exercises.Select(SummaryExerciseItem.Create));
        TotalVolume = summary.TotalVolume;
        TotalSets = summary.Exercises.Sum(exercise => exercise.Sets.Count);
        DateText = startTimeUtc.ToLocalTime().ToString(UiText.SummaryDateFormat, CultureInfo.InvariantCulture);
        IsVisible = true;
    }

    public void Clear()
    {
        IsVisible = false;
        Exercises.Clear();
        TotalVolume = 0;
        TotalSets = 0;
        DateText = string.Empty;
        TotalTimeText = ZeroTimeText;
    }
}
