using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using MuscleMemory.Constants;
using MuscleMemory.Data.Repositories;
using MuscleMemory.Services;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

public partial class WorkoutHistoryViewModel(
    IWorkoutHistoryQueryService historyQueryService,
    ISessionExerciseRepository sessionExerciseRepository,
    IWorkoutSetRepository setRepository,
    IExerciseRepository exerciseRepository) : ObservableObject, IQueryAttributable
{
    private readonly IWorkoutHistoryQueryService _historyQueryService = historyQueryService;
    private readonly ISessionExerciseRepository _sessionExerciseRepository = sessionExerciseRepository;
    private readonly IWorkoutSetRepository _setRepository = setRepository;
    private readonly IExerciseRepository _exerciseRepository = exerciseRepository;
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

        if (query.TryGetValue(QueryKeys.WorkoutId, out var id)
            && int.TryParse(id?.ToString(), out int workoutId)
            && workoutId > 0)
        {
            _workoutId = workoutId;
            _ = LoadHistoryAsync();
        }
    }

    private async Task LoadHistoryAsync()
    {
        var history = await _historyQueryService.GetWorkoutHistoryAsync(_workoutId);
        Sessions.Clear();
        foreach (var session in history)
        {
            Sessions.Add(session);
        }
        IsEmpty = !Sessions.Any();
    }

    [RelayCommand]
    private async Task EditSetAsync(WorkoutSet set)
    {
        if (set == null) return;

        string weightStr = await Shell.Current.DisplayPromptAsync(UiText.TitleEditSet, UiText.PromptEnterWeightKg, initialValue: set.Weight.ToString(), keyboard: Keyboard.Numeric);
        if (weightStr == null) return;

        string repsStr = await Shell.Current.DisplayPromptAsync(UiText.TitleEditSet, UiText.PromptEnterReps, initialValue: set.Reps.ToString(), keyboard: Keyboard.Numeric);
        if (repsStr == null) return;

        if (double.TryParse(weightStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double newWeight) && int.TryParse(repsStr, out int newReps))
        {
            set.Weight = newWeight;
            set.Reps = newReps;

            await _setRepository.UpdateAsync(set);
            await LoadHistoryAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteSetAsync(WorkoutSet set)
    {
        if (set == null) return;
        bool confirm = await Shell.Current.DisplayAlertAsync(UiText.TitleDeleteSet, UiText.BodyDeleteSetConfirmation, UiText.ButtonYes, UiText.ButtonNo);
        if (!confirm) return;

        await _setRepository.DeleteAsync(set.Id);
        await LoadHistoryAsync();
    }

    [RelayCommand]
    private async Task AddSetAsync(WorkoutHistoryExercise loggedExercise)
    {
        if (loggedExercise == null) return;

        var lastSet = loggedExercise.Sets.LastOrDefault();
        double weight = lastSet?.Weight ?? 0;
        int reps = lastSet?.Reps ?? 0;

        string weightStr = await Shell.Current.DisplayPromptAsync(UiText.TitleAddSet, UiText.PromptEnterWeightKg, initialValue: weight.ToString(), keyboard: Keyboard.Numeric);
        if (weightStr == null) return;

        string repsStr = await Shell.Current.DisplayPromptAsync(UiText.TitleAddSet, UiText.PromptEnterReps, initialValue: reps.ToString(), keyboard: Keyboard.Numeric);
        if (repsStr == null) return;

        if (double.TryParse(weightStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double newWeight) && int.TryParse(repsStr, out int newReps))
        {
            await _setRepository.AddAsync(new WorkoutSet
            {
                SessionExerciseId = loggedExercise.SessionExerciseId,
                SetNumber = loggedExercise.Sets.Count + 1,
                Weight = newWeight,
                Reps = newReps
            });

            await LoadHistoryAsync();
        }
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
            SetNumber = 1,
            Weight = 0,
            Reps = 0
        });

        await LoadHistoryAsync();
    }
}
