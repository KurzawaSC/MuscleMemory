using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using MuscleMemory.Constants;
using MuscleMemory.Data.Repositories;
using MuscleMemory.Extensions;
using MuscleMemory.Services;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

public partial class WorkoutHistoryViewModel(
    IWorkoutHistoryQueryService historyQueryService,
    ISessionExerciseRepository sessionExerciseRepository,
    IWorkoutSetRepository setRepository,
    IDialogService dialogs,
    IWorkoutTimerService timer,
    INavigationService navigation,
    IErrorHandler errors,
    SelectExerciseViewModel exercisePicker) : ObservableObject, IQueryAttributable
{
    private readonly IWorkoutHistoryQueryService _historyQueryService = historyQueryService;
    private readonly ISessionExerciseRepository _sessionExerciseRepository = sessionExerciseRepository;
    private readonly IWorkoutSetRepository _setRepository = setRepository;
    private readonly IDialogService _dialogs = dialogs;
    private readonly IWorkoutTimerService _timer = timer;
    private readonly INavigationService _navigation = navigation;
    private readonly IErrorHandler _errors = errors;
    private int _workoutId;
    private int _latestLoad;
    private WorkoutSet? _setBeingEdited;
    private WorkoutHistoryExercise? _exerciseReceivingSet;

    [ObservableProperty]
    public partial string WorkoutName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

    public ObservableCollection<HistorySessionItem> Sessions { get; } = [];

    [ObservableProperty]
    public partial HistorySessionItem? SelectedSession { get; set; }

    public SelectExerciseViewModel ExercisePicker { get; } = exercisePicker;

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

    public string ActionSetTitle => ActionSet is { } set ? string.Format(UiText.SetProgressFormat, set.SetNumber) : string.Empty;

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
            _errors.ReportFailures(LoadHistoryAsync());
        }
    }

    private async Task LoadHistoryAsync()
    {
        var load = ++_latestLoad;
        var history = await _historyQueryService.GetWorkoutHistoryAsync(_workoutId);
        if (load != _latestLoad)
        {
            return;
        }

        var selectedSessionId = SelectedSession?.Session.SessionId;

        Sessions.ReplaceAll(history.Select(session => HistorySessionItem.Create(session, _timer.FormatElapsed(session.Duration))));
        SelectedSession = Sessions.FirstOrDefault(item => item.Session.SessionId == selectedSessionId) ?? Sessions.FirstOrDefault();
        IsEmpty = Sessions.Count == 0;
    }

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

        if (!await _dialogs.ConfirmAsync(UiText.TitleDeleteSet, UiText.BodyDeleteSetConfirmation, UiText.ButtonDelete, UiText.ButtonCancel)) return;

        await _setRepository.DeleteAsync(set.Id);
        await LoadHistoryAsync();
    });

    private async Task<WorkoutSet?> DismissSetActionsAsync()
    {
        var set = ActionSet;
        CancelSetActions();
        await WaitForSheetToCloseAsync();
        return set;
    }

    [RelayCommand]
    private void AddSet(WorkoutHistoryExercise loggedExercise)
    {
        if (loggedExercise == null) return;

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
        if (!SetEditor.TryRead(out var values))
        {
            return;
        }

        if (_setBeingEdited is { } editedSet)
        {
            await _setRepository.UpdateAsync(editedSet.Id, values.Weight, values.Reps);
        }
        else if (_exerciseReceivingSet is { } receivingExercise)
        {
            await _setRepository.AddAsync(new WorkoutSet
            {
                SessionExerciseId = receivingExercise.SessionExerciseId,
                Weight = values.Weight,
                Reps = values.Reps
            });
        }

        CloseSetEditor();
        await LoadHistoryAsync();
    });

    [RelayCommand]
    private Task DeleteExerciseAsync(WorkoutHistoryExercise loggedExercise) => _errors.RunAsync(async () =>
    {
        if (loggedExercise == null) return;
        bool confirm = await _dialogs.ConfirmAsync(UiText.TitleDeleteExercise, string.Format(UiText.RemoveExerciseConfirmationFormat, loggedExercise.ExerciseName), UiText.ButtonDelete, UiText.ButtonCancel);
        if (!confirm) return;

        await _sessionExerciseRepository.DeleteAsync(loggedExercise.SessionExerciseId);
        await LoadHistoryAsync();
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

        await LoadHistoryAsync();
    });

    private static Task WaitForSheetToCloseAsync() =>
        Task.Delay(TimeSpan.FromMilliseconds(UiTiming.SheetCloseMilliseconds));
}
