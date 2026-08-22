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
    IExerciseRepository exerciseRepository,
    ISetEditService setEditService) : ObservableObject, IQueryAttributable
{
    private readonly IWorkoutHistoryQueryService _historyQueryService = historyQueryService;
    private readonly ISessionExerciseRepository _sessionExerciseRepository = sessionExerciseRepository;
    private readonly IWorkoutSetRepository _setRepository = setRepository;
    private readonly IExerciseRepository _exerciseRepository = exerciseRepository;
    private readonly ISetEditService _setEditService = setEditService;
    private int _workoutId;

    [ObservableProperty]
    public partial string WorkoutName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

    public ObservableCollection<WorkoutHistorySession> Sessions { get; } = [];

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
        Sessions.ReplaceAll(await _historyQueryService.GetWorkoutHistoryAsync(_workoutId));
        IsEmpty = !Sessions.Any();
    }

    [RelayCommand]
    private async Task EditSetAsync(WorkoutSet set)
    {
        if (set == null) return;

        var values = await _setEditService.PromptForSetAsync(UiText.TitleEditSet, set.Weight, set.Reps);
        if (values is null) return;

        set.Weight = values.Weight;
        set.Reps = values.Reps;

        await _setRepository.UpdateAsync(set);
        await LoadHistoryAsync();
    }

    [RelayCommand]
    private async Task DeleteSetAsync(WorkoutSet set)
    {
        if (set == null) return;
        if (!await _setEditService.ConfirmDeleteAsync()) return;

        await _setRepository.DeleteAsync(set.Id);
        await LoadHistoryAsync();
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
    private async Task AddExerciseAsync(WorkoutHistorySession session)
    {
        if (session == null) return;

        var allExercises = await _exerciseRepository.GetAllAsync();
        if (!allExercises.Any())
        {
            await Shell.Current.DisplayAlertAsync(UiText.TitleNoExercises, UiText.BodyNoExercisesInLibrary, UiText.ButtonOk);
            return;
        }

        var exerciseNames = allExercises.Select(exercise => exercise.Name).ToArray();
        string selectedName = await Shell.Current.DisplayActionSheetAsync(UiText.TitleSelectExercise, UiText.ButtonCancel, null, exerciseNames);

        if (string.IsNullOrEmpty(selectedName) || selectedName == UiText.ButtonCancel)
            return;

        var selectedExercise = allExercises.First(exercise => exercise.Name == selectedName);

        var addedExercise = await _sessionExerciseRepository.AppendToSessionAsync(new SessionExercise
        {
            WorkoutSessionId = session.SessionId,
            ExerciseId = selectedExercise.Id,
            ExerciseName = selectedExercise.Name,
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
}
