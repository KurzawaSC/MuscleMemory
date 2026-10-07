using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Extensions;
using MuscleMemory.Services;

namespace MuscleMemory.ViewModels;

public partial class ExerciseHistoryViewModel(
    IWorkoutHistoryQueryService historyQueryService,
    INavigationService navigation,
    IErrorHandler errors) : ObservableObject, IQueryAttributable
{
    private int _exerciseId;

    [ObservableProperty]
    public partial string ExerciseName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SessionCountText { get; set; } = string.Empty;

    public ListLoadState ListState { get; } = new();

    public ObservableCollection<ExerciseHistoryItem> History { get; } = [];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(QueryKeys.ExerciseName, out var name) && name is string exerciseName)
        {
            ExerciseName = exerciseName;
        }

        if (query.TryGetValue(QueryKeys.ExerciseId, out var id) && id is int exerciseId && exerciseId > 0)
        {
            _exerciseId = exerciseId;
            errors.ReportFailures(LoadHistoryAsync());
        }
    }

    private async Task LoadHistoryAsync()
    {
        var entries = await historyQueryService.GetExerciseHistoryAsync(_exerciseId);
        History.ReplaceAll(entries.Select(ExerciseHistoryItem.Create));
        ListState.Complete(History.Count);
        SessionCountText = ListState.IsEmpty ? string.Empty : FormatSessionCount(History.Count);
    }

    private static string FormatSessionCount(int count) =>
        string.Format(CultureInfo.CurrentCulture, UiText.SessionCountFormat, count, CountCaption.Sessions(count));

    [RelayCommand]
    private Task GoBackAsync() =>
        errors.RunAsync(() => navigation.GoToAsync(NavigationRoutes.GoBack));
}
