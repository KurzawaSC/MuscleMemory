using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Extensions;
using MuscleMemory.Models;
using MuscleMemory.Resources.Strings;
using MuscleMemory.Services;
using MuscleMemory.Threading;

namespace MuscleMemory.ViewModels;

public partial class WorkoutHistoryViewModel : ObservableObject, IQueryAttributable, ISetActionsHost
{
    private readonly IHistoryQueryService _historyQueryService;
    private readonly IActiveWorkoutSessionService _sessions;
    private readonly IDialogService _dialogs;
    private readonly IWorkoutTimerService _timer;
    private readonly IHapticService _haptics;
    private readonly INavigationService _navigation;
    private readonly IErrorHandler _errors;
    private readonly TimeProvider _clock;
    private readonly SequentialTaskQueue _historyUpdates = new();
    private int _workoutId;

    public WorkoutHistoryViewModel(
        IHistoryQueryService historyQueryService,
        IActiveWorkoutSessionService sessions,
        IDialogService dialogs,
        IWorkoutTimerService timer,
        IHapticService haptics,
        INavigationService navigation,
        IErrorHandler errors,
        TimeProvider clock,
        ExercisePickerViewModel exercisePicker)
    {
        _historyQueryService = historyQueryService;
        _sessions = sessions;
        _dialogs = dialogs;
        _timer = timer;
        _haptics = haptics;
        _navigation = navigation;
        _errors = errors;
        _clock = clock;
        ExercisePicker = exercisePicker;
        SetActions = new SetActionsViewModel(this, dialogs, errors, clock);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeaderTitle))]
    public partial string WorkoutName { get; set; } = string.Empty;

    public string HeaderTitle => string.Format(CultureInfo.CurrentCulture, AppResources.WorkoutHistoryTitleFormat, WorkoutName);

    public ListLoadState ListState { get; } = new();

    public ObservableCollection<WorkoutHistoryItem> Sessions { get; } = [];

    [ObservableProperty]
    public partial WorkoutHistoryItem? SelectedSession { get; set; }

    public ExercisePickerViewModel ExercisePicker { get; }

    [ObservableProperty]
    public partial bool IsExercisePickerOpen { get; set; }

    [ObservableProperty]
    public partial bool IsExerciseActionSheetOpen { get; set; }

    [ObservableProperty]
    public partial WorkoutHistoryExercise? ActionExercise { get; set; }

    public SetActionsViewModel SetActions { get; }

    private bool IsAnySheetOpen => IsExercisePickerOpen || SetActions.IsAnySheetOpen || IsExerciseActionSheetOpen;

    bool ISetActionsHost.CanShowSetActions => true;

    Task ISetActionsHost.UpdateSetAsync(WorkoutSet set, SetValues values) =>
        _sessions.UpdateSetAsync(set.Id, values.Weight, values.Reps);

    async Task ISetActionsHost.DeleteSetAsync(WorkoutSet set)
    {
        var sessionId = SessionIdOf(set.SessionExerciseId);
        await _sessions.DeleteSetAsync(set.Id);
        await RefreshSessionAsync(sessionId);
    }

    Task ISetActionsHost.RefreshSetsAsync(int sessionExerciseId) => RefreshSessionAsync(SessionIdOf(sessionExerciseId));

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(QueryKeys.WorkoutName, out var name) && name is string workoutName)
        {
            WorkoutName = workoutName;
        }

        if (query.TryGetValue(QueryKeys.WorkoutId, out var id) && id is int workoutId && workoutId > 0)
        {
            _workoutId = workoutId;
            _errors.ReportFailures(_historyUpdates.EnqueueAsync(LoadHistoryAsync));
        }
    }

    private async Task LoadHistoryAsync()
    {
        var history = await _historyQueryService.GetWorkoutHistoryAsync(_workoutId);
        var selectedSessionId = SelectedSession?.Session.SessionId;

        var sharedDates = FindSharedDates(history);
        Sessions.ReplaceAll(history.Select(session => CreateItem(session, sharedDates.Contains(session.LocalStartTime.Date))));
        RestoreSelection(selectedSessionId);
    }

    private static HashSet<DateTime> FindSharedDates(IEnumerable<WorkoutHistorySession> sessions) =>
        [.. sessions.CountBy(session => session.LocalStartTime.Date).Where(day => day.Value > 1).Select(day => day.Key)];

    private Task RefreshSessionAsync(int sessionId) =>
        _historyUpdates.EnqueueAsync(() => ReloadSessionAsync(sessionId));

    private async Task ReloadSessionAsync(int sessionId)
    {
        var session = await _historyQueryService.GetWorkoutHistorySessionAsync(sessionId);
        if (Sessions.FirstOrDefault(item => item.Session.SessionId == sessionId) is not { } current)
        {
            return;
        }

        var selectedSessionId = SelectedSession?.Session.SessionId;

        if (session is null)
        {
            RemoveSession(current);
        }
        else
        {
            Sessions[Sessions.IndexOf(current)] = CreateItem(session, SessionsOn(session.LocalStartTime.Date).Count > 1);
        }

        RestoreSelection(selectedSessionId);
    }

    private void RemoveSession(WorkoutHistoryItem removed)
    {
        Sessions.Remove(removed);

        if (SessionsOn(removed.Session.LocalStartTime.Date) is [var formerSibling])
        {
            Sessions[Sessions.IndexOf(formerSibling)] = CreateItem(formerSibling.Session, sharesDate: false);
        }
    }

    private List<WorkoutHistoryItem> SessionsOn(DateTime localDate) =>
        [.. Sessions.Where(item => item.Session.LocalStartTime.Date == localDate)];

    private WorkoutHistoryItem CreateItem(WorkoutHistorySession session, bool sharesDate) =>
        WorkoutHistoryItem.Create(session, _timer.FormatDuration(session.Duration), sharesDate);

    private void RestoreSelection(int? sessionId)
    {
        SelectedSession = Sessions.FirstOrDefault(item => item.Session.SessionId == sessionId) ?? Sessions.FirstOrDefault();
        ListState.Complete(Sessions.Count);
    }

    private int SessionIdOf(int sessionExerciseId) =>
        Sessions.First(item => item.Session.Exercises.Any(exercise => exercise.SessionExerciseId == sessionExerciseId)).Session.SessionId;

    [RelayCommand]
    private Task NavigateBackAsync() => _errors.RunAsync(async () =>
    {
        if (IsAnySheetOpen)
        {
            CloseSheets();
            return;
        }

        await _navigation.GoToAsync(NavigationRoutes.GoBack);
    });

    private void CloseSheets()
    {
        IsExercisePickerOpen = false;
        SetActions.CloseSheets();
        CancelExerciseActions();
    }

    [RelayCommand]
    private void AddSet(WorkoutHistoryExerciseItem item) =>
        SetActions.OpenEditor(
            AppResources.TitleAddSet,
            AppResources.ButtonAdd,
            item.Exercise.Sets.LastOrDefault(),
            item.Exercise.SessionExerciseId,
            values => AddSetAsync(item.Exercise.SessionExerciseId, values));

    private Task AddSetAsync(int sessionExerciseId, SetValues values) =>
        _sessions.AddSetAsync(new WorkoutSet
        {
            SessionExerciseId = sessionExerciseId,
            Weight = values.Weight,
            Reps = values.Reps
        });

    [RelayCommand]
    private void ShowExerciseActions(WorkoutHistoryExerciseItem item)
    {
        ActionExercise = item.Exercise;
        IsExerciseActionSheetOpen = true;
    }

    [RelayCommand]
    private void LongPressExercise(WorkoutHistoryExerciseItem item)
    {
        _haptics.Click();
        ShowExerciseActions(item);
    }

    [RelayCommand]
    private void CancelExerciseActions()
    {
        IsExerciseActionSheetOpen = false;
        ActionExercise = null;
    }

    private async Task<WorkoutHistoryExercise?> DismissExerciseActionsAsync()
    {
        var loggedExercise = ActionExercise;
        await SheetTransition.CloseAsync(CancelExerciseActions, _clock);
        return loggedExercise;
    }

    [RelayCommand]
    private Task DeleteActionExerciseAsync() => _errors.RunAsync(async () =>
    {
        if (await DismissExerciseActionsAsync() is not { } loggedExercise)
        {
            return;
        }

        var confirm = await _dialogs.ConfirmAsync(AppResources.TitleRemoveExercise, string.Format(AppResources.RemoveExerciseConfirmationFormat, loggedExercise.ExerciseName), AppResources.ButtonRemove, AppResources.ButtonCancel);
        if (!confirm)
        {
            return;
        }

        var sessionId = SessionIdOf(loggedExercise.SessionExerciseId);
        await _sessions.RemoveExerciseAsync(loggedExercise.SessionExerciseId);
        await RefreshSessionAsync(sessionId);
    });

    [RelayCommand]
    private Task AddExerciseAsync() => _errors.RunAsync(async () =>
    {
        await ExercisePicker.LoadAsync();
        IsExercisePickerOpen = true;
    });

    [RelayCommand]
    private void CloseExercisePicker()
    {
        IsExercisePickerOpen = false;
    }

    [RelayCommand]
    private Task PickExerciseAsync(Exercise exercise) => _errors.RunAsync(async () =>
    {
        IsExercisePickerOpen = false;

        if (SelectedSession is not { } selected)
        {
            return;
        }

        await _sessions.AppendExerciseAsync(selected.Session.SessionId, exercise);
        await RefreshSessionAsync(selected.Session.SessionId);
    });
}
