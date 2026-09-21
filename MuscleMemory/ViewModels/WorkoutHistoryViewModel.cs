using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
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
    ISetEditService setEditService,
    IWorkoutTimerService timer,
    SelectExerciseViewModel exercisePicker) : ObservableObject, IQueryAttributable
{
    private readonly IWorkoutHistoryQueryService _historyQueryService = historyQueryService;
    private readonly ISessionExerciseRepository _sessionExerciseRepository = sessionExerciseRepository;
    private readonly IWorkoutSetRepository _setRepository = setRepository;
    private readonly ISetEditService _setEditService = setEditService;
    private readonly IWorkoutTimerService _timer = timer;
    private int _workoutId;

    [ObservableProperty]
    public partial string WorkoutName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

    public ObservableCollection<HistorySessionItem> Sessions { get; } = [];

    [ObservableProperty]
    public partial HistorySessionItem? SelectedSession { get; set; }

    public SelectExerciseViewModel ExercisePicker { get; } = exercisePicker;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseSheetsCommand))]
    public partial bool IsExercisePickerOpen { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseSheetsCommand))]
    public partial bool IsSetActionSheetOpen { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActionSetTitle))]
    [NotifyPropertyChangedFor(nameof(ActionSetSubtitle))]
    public partial WorkoutSet? ActionSet { get; set; }

    public string ActionSetTitle => ActionSet is { } set ? string.Format(UiText.SetProgressFormat, set.SetNumber) : string.Empty;

    public string ActionSetSubtitle => ActionSet is { } set ? string.Format(UiText.LoggedSetFormat, set.Weight, set.Reps) : string.Empty;

    private bool IsAnySheetOpen => IsExercisePickerOpen || IsSetActionSheetOpen;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(QueryKeys.WorkoutName, out var name) && name is string workoutName)
        {
            WorkoutName = workoutName;
        }

        if (query.TryGetValue(QueryKeys.WorkoutId, out var id) && id is int workoutId && workoutId > 0)
        {
            _workoutId = workoutId;
            _ = LoadHistoryAsync();
        }
    }

    private async Task LoadHistoryAsync()
    {
        var selectedSessionId = SelectedSession?.Session.SessionId;
        var history = await _historyQueryService.GetWorkoutHistoryAsync(_workoutId);

        Sessions.ReplaceAll(history.Select(session => HistorySessionItem.Create(session, _timer.FormatElapsed(session.Duration))));
        SelectedSession = Sessions.FirstOrDefault(item => item.Session.SessionId == selectedSessionId) ?? Sessions.FirstOrDefault();
        IsEmpty = Sessions.Count == 0;
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync(NavigationRoutes.GoBack);
    }

    [RelayCommand(CanExecute = nameof(IsAnySheetOpen))]
    private void CloseSheets()
    {
        IsExercisePickerOpen = false;
        CancelSetActions();
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
    private async Task EditActionSetAsync()
    {
        if (await DismissSetActionsAsync() is not { } set)
        {
            return;
        }

        var values = await _setEditService.PromptForSetAsync(UiText.TitleEditSet, set.Weight, set.Reps);
        if (values is null) return;

        set.Weight = values.Weight;
        set.Reps = values.Reps;

        await _setRepository.UpdateAsync(set);
        await LoadHistoryAsync();
    }

    [RelayCommand]
    private async Task DeleteActionSetAsync()
    {
        if (await DismissSetActionsAsync() is not { } set)
        {
            return;
        }

        if (!await _setEditService.ConfirmDeleteAsync()) return;

        await _setRepository.DeleteAsync(set.Id);
        await LoadHistoryAsync();
    }

    private async Task<WorkoutSet?> DismissSetActionsAsync()
    {
        var set = ActionSet;
        CancelSetActions();
        await WaitForSheetToCloseAsync();
        return set;
    }

    [RelayCommand]
    private async Task AddSetAsync(WorkoutHistoryExercise loggedExercise)
    {
        if (loggedExercise == null) return;

        var lastSet = loggedExercise.Sets.LastOrDefault();

        var values = await _setEditService.PromptForSetAsync(UiText.TitleAddSet, lastSet?.Weight ?? 0, lastSet?.Reps ?? 0);
        if (values is null) return;

        await _setRepository.AddAsync(new WorkoutSet
        {
            SessionExerciseId = loggedExercise.SessionExerciseId,
            Weight = values.Weight,
            Reps = values.Reps
        });

        await LoadHistoryAsync();
    }

    [RelayCommand]
    private async Task DeleteExerciseAsync(WorkoutHistoryExercise loggedExercise)
    {
        if (loggedExercise == null) return;
        bool confirm = await Shell.Current.DisplayAlertAsync(UiText.TitleDeleteExercise, string.Format(UiText.RemoveExerciseConfirmationFormat, loggedExercise.ExerciseName), UiText.ButtonYes, UiText.ButtonNo);
        if (!confirm) return;

        await _setRepository.DeleteForSessionExerciseAsync(loggedExercise.SessionExerciseId);
        await _sessionExerciseRepository.DeleteAsync(loggedExercise.SessionExerciseId);
        await LoadHistoryAsync();
    }

    [RelayCommand]
    private async Task AddExerciseAsync()
    {
        await ExercisePicker.LoadAsync();
        IsExercisePickerOpen = true;
    }

    [RelayCommand]
    private void CloseExercisePicker()
    {
        IsExercisePickerOpen = false;
    }

    [RelayCommand]
    private async Task PickExerciseAsync(Exercise exercise)
    {
        IsExercisePickerOpen = false;

        if (SelectedSession is not { } selected)
        {
            return;
        }

        var addedExercise = await _sessionExerciseRepository.AppendToSessionAsync(new SessionExercise
        {
            WorkoutSessionId = selected.Session.SessionId,
            ExerciseId = exercise.Id,
            ExerciseName = exercise.Name,
            PlannedSets = DomainDefaults.Sets,
            PlannedReps = DomainDefaults.Reps,
            BreakTimeInSeconds = DomainDefaults.BreakTimeInSeconds,
            TargetRPE = DomainDefaults.TargetRPE
        });

        await _setRepository.AddAsync(new WorkoutSet
        {
            SessionExerciseId = addedExercise.Id,
            Weight = 0,
            Reps = 0
        });

        await LoadHistoryAsync();
    }

    private static Task WaitForSheetToCloseAsync() =>
        Task.Delay(TimeSpan.FromMilliseconds(UiTiming.SheetCloseMilliseconds));
}
