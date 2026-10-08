using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Data.Repositories;
using MuscleMemory.Extensions;
using MuscleMemory.Models;
using MuscleMemory.Services;
using MuscleMemory.Threading;

namespace MuscleMemory.ViewModels;

public partial class WorkoutHistoryViewModel : ObservableObject, IQueryAttributable
{
    private readonly IHistoryQueryService _historyQueryService;
    private readonly ISessionExerciseRepository _sessionExerciseRepository;
    private readonly IWorkoutSetRepository _setRepository;
    private readonly IDialogService _dialogs;
    private readonly IWorkoutTimerService _timer;
    private readonly INavigationService _navigation;
    private readonly IErrorHandler _errors;
    private readonly SequentialTaskQueue _historyUpdates = new();
    private int _workoutId;
    private WorkoutSet? _setBeingEdited;
    private WorkoutHistoryExercise? _exerciseReceivingSet;

    public WorkoutHistoryViewModel(
        IHistoryQueryService historyQueryService,
        ISessionExerciseRepository sessionExerciseRepository,
        IWorkoutSetRepository setRepository,
        IDialogService dialogs,
        IWorkoutTimerService timer,
        INavigationService navigation,
        IErrorHandler errors,
        ExercisePickerViewModel exercisePicker)
    {
        _historyQueryService = historyQueryService;
        _sessionExerciseRepository = sessionExerciseRepository;
        _setRepository = setRepository;
        _dialogs = dialogs;
        _timer = timer;
        _navigation = navigation;
        _errors = errors;
        ExercisePicker = exercisePicker;
    }

    [ObservableProperty]
    public partial string WorkoutName { get; set; } = string.Empty;

    public ListLoadState ListState { get; } = new();

    public ObservableCollection<WorkoutHistoryItem> Sessions { get; } = [];

    [ObservableProperty]
    public partial WorkoutHistoryItem? SelectedSession { get; set; }

    public ExercisePickerViewModel ExercisePicker { get; }

    [ObservableProperty]
    public partial bool IsExercisePickerOpen { get; set; }

    [ObservableProperty]
    public partial bool IsSetActionSheetOpen { get; set; }

    [ObservableProperty]
    public partial bool IsSetEditorOpen { get; set; }

    [ObservableProperty]
    public partial string SetEditorTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SetEditorConfirmText { get; set; } = string.Empty;

    public SetInputViewModel SetEditor { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActionSetTitle))]
    [NotifyPropertyChangedFor(nameof(ActionSetSubtitle))]
    public partial WorkoutSet? ActionSet { get; set; }

    public string ActionSetTitle => ActionSet is { } set ? string.Format(CultureInfo.CurrentCulture, UiText.SetProgressFormat, set.SetNumber) : string.Empty;

    public string ActionSetSubtitle => ActionSet is { } set ? string.Format(CultureInfo.CurrentCulture, UiText.LoggedSetFormat, set.Weight, set.Reps) : string.Empty;

    private bool IsAnySheetOpen => IsExercisePickerOpen || IsSetActionSheetOpen || IsSetEditorOpen;

    partial void OnIsSetEditorOpenChanged(bool value)
    {
        if (!value)
        {
            _setBeingEdited = null;
            _exerciseReceivingSet = null;
        }
    }

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
        CancelSetActions();
        CloseSetEditor();
    }

    [RelayCommand]
    private void ShowSetActions(WorkoutSet set)
    {
        ActionSet = set;
        IsSetActionSheetOpen = true;
    }

    [RelayCommand]
    private void CancelSetActions()
    {
        IsSetActionSheetOpen = false;
        ActionSet = null;
    }

    [RelayCommand]
    private Task EditActionSetAsync() => _errors.RunAsync(async () =>
    {
        if (await DismissSetActionsAsync() is not { } set)
        {
            return;
        }

        _setBeingEdited = set;
        SetEditor.Fill(set.Weight, set.Reps);
        OpenSetEditor(UiText.TitleEditSet, UiText.ButtonSave);
    });

    [RelayCommand]
    private Task DeleteActionSetAsync() => _errors.RunAsync(async () =>
    {
        if (await DismissSetActionsAsync() is not { } set)
        {
            return;
        }

        if (!await _dialogs.ConfirmAsync(UiText.TitleDeleteSet, UiText.BodyDeleteSetConfirmation, UiText.ButtonDelete, UiText.ButtonCancel))
        {
            return;
        }

        var sessionId = SessionIdOf(set.SessionExerciseId);
        await _setRepository.DeleteAsync(set.Id);
        await RefreshSessionAsync(sessionId);
    });

    private async Task<WorkoutSet?> DismissSetActionsAsync()
    {
        var set = ActionSet;
        await SheetTransition.CloseAsync(CancelSetActions);
        return set;
    }

    [RelayCommand]
    private void AddSet(WorkoutHistoryExercise loggedExercise)
    {
        _setBeingEdited = null;
        _exerciseReceivingSet = loggedExercise;

        if (loggedExercise.Sets.LastOrDefault() is { } lastSet)
        {
            SetEditor.Fill(lastSet.Weight, lastSet.Reps);
        }
        else
        {
            SetEditor.Clear();
        }

        OpenSetEditor(UiText.TitleAddSet, UiText.ButtonAdd);
    }

    private void OpenSetEditor(string title, string confirmText)
    {
        SetEditorTitle = title;
        SetEditorConfirmText = confirmText;
        IsSetEditorOpen = true;
    }

    [RelayCommand]
    private void CloseSetEditor()
    {
        IsSetEditorOpen = false;
    }

    [RelayCommand]
    private Task SaveSetEditorAsync() => _errors.RunAsync(async () =>
    {
        if (!SetEditor.TryRead(out var values)
            || (_setBeingEdited?.SessionExerciseId ?? _exerciseReceivingSet?.SessionExerciseId) is not { } sessionExerciseId)
        {
            return;
        }

        var sessionId = SessionIdOf(sessionExerciseId);
        await SaveEditorSetAsync(sessionExerciseId, values);
        CloseSetEditor();
        await RefreshSessionAsync(sessionId);
    });

    private Task SaveEditorSetAsync(int sessionExerciseId, SetValues values) =>
        _setBeingEdited is { } editedSet
            ? _setRepository.UpdateAsync(editedSet.Id, values.Weight, values.Reps)
            : _setRepository.AddAsync(new WorkoutSet
            {
                SessionExerciseId = sessionExerciseId,
                Weight = values.Weight,
                Reps = values.Reps
            });

    [RelayCommand]
    private Task DeleteExerciseAsync(WorkoutHistoryExercise loggedExercise) => _errors.RunAsync(async () =>
    {
        var confirm = await _dialogs.ConfirmAsync(UiText.TitleRemoveExercise, string.Format(UiText.RemoveExerciseConfirmationFormat, loggedExercise.ExerciseName), UiText.ButtonRemove, UiText.ButtonCancel);
        if (!confirm)
        {
            return;
        }

        var sessionId = SessionIdOf(loggedExercise.SessionExerciseId);
        await _sessionExerciseRepository.DeleteAsync(loggedExercise.SessionExerciseId);
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

        await _sessionExerciseRepository.AppendToSessionAsync(new SessionExercise
        {
            WorkoutSessionId = selected.Session.SessionId,
            ExerciseId = exercise.Id,
            ExerciseName = exercise.Name,
            PlannedSets = DomainDefaults.Sets,
            PlannedReps = DomainDefaults.Reps,
            BreakTimeInSeconds = DomainDefaults.BreakTimeInSeconds,
            TargetRPE = DomainDefaults.TargetRPE
        });

        await RefreshSessionAsync(selected.Session.SessionId);
    });
}
