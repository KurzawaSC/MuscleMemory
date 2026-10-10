using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using MuscleMemory.Extensions;
using MuscleMemory.Localization;
using MuscleMemory.Resources.Strings;
using MuscleMemory.Services;

namespace MuscleMemory.ViewModels;

public sealed partial class WorkoutSummaryViewModel : ObservableObject
{
    private readonly IWorkoutTimerService _timer;
    private WorkoutSummary? _summary;
    private DateTime _startTimeUtc;

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

    public string TotalSetsCaption => Plurals.Format(nameof(AppResources.SetsCaption_Other), TotalSets);

    [ObservableProperty]
    public partial string DateText { get; private set; } = string.Empty;

    public ObservableCollection<SummaryExerciseItem> Exercises { get; } = [];

    public void Show(WorkoutSummary summary, DateTime startTimeUtc)
    {
        _summary = summary;
        _startTimeUtc = startTimeUtc;
        TotalTimeText = _timer.ElapsedSince(startTimeUtc);
        TotalVolume = summary.TotalVolume;
        TotalSets = summary.Exercises.Sum(exercise => exercise.Sets.Count);
        ShowText(summary);
        IsVisible = true;
    }

    public void RefreshText()
    {
        if (_summary is { } summary)
        {
            ShowText(summary);
        }
    }

    private void ShowText(WorkoutSummary summary)
    {
        Exercises.ReplaceAll(summary.Exercises.Select(SummaryExerciseItem.Create));
        DateText = _startTimeUtc.ToLocalTime().ToString(AppResources.SummaryDateFormat, CultureInfo.InvariantCulture);
        OnPropertyChanged(nameof(TotalSetsCaption));
    }

    public void Clear()
    {
        _summary = null;
        IsVisible = false;
        Exercises.Clear();
        TotalVolume = 0;
        TotalSets = 0;
        DateText = string.Empty;
        TotalTimeText = ZeroTimeText;
    }
}
