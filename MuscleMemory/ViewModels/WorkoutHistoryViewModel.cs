using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using MuscleMemory.Constants;
using MuscleMemory.Data;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

public partial class WorkoutHistoryViewModel(DatabaseContext dbContext) : ObservableObject, IQueryAttributable
{
    private readonly DatabaseContext _dbContext = dbContext;
    private int _workoutId;

    [ObservableProperty]
    public partial string WorkoutName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

    public ObservableCollection<WorkoutHistorySession> Sessions { get; } = [];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(QueryKeys.WorkoutName, out var name))
        {
            WorkoutName = name?.ToString() ?? string.Empty;
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
        var history = await _dbContext.GetWorkoutHistoryAsync(_workoutId);
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
            
            await _dbContext.UpdateSetAsync(set);
            await LoadHistoryAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteSetAsync(WorkoutSet set)
    {
        if (set == null) return;
        bool confirm = await Shell.Current.DisplayAlertAsync(UiText.TitleDeleteSet, UiText.BodyDeleteSetConfirmation, UiText.ButtonYes, UiText.ButtonNo);
        if (!confirm) return;

        await _dbContext.DeleteSetAsync(set.Id);
        await LoadHistoryAsync();
    }

    [RelayCommand]
    private async Task AddSetAsync(WorkoutHistoryExercise parentExercise)
    {
        if (parentExercise == null) return;

        var lastSet = parentExercise.Sets.LastOrDefault();
        double weight = lastSet?.Weight ?? 0;
        int reps = lastSet?.Reps ?? 0;

        string weightStr = await Shell.Current.DisplayPromptAsync(UiText.TitleAddSet, UiText.PromptEnterWeightKg, initialValue: weight.ToString(), keyboard: Keyboard.Numeric);
        if (weightStr == null) return;

        string repsStr = await Shell.Current.DisplayPromptAsync(UiText.TitleAddSet, UiText.PromptEnterReps, initialValue: reps.ToString(), keyboard: Keyboard.Numeric);
        if (repsStr == null) return;

        if (double.TryParse(weightStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double newWeight) && int.TryParse(repsStr, out int newReps))
        {
            var newSet = new WorkoutSet
            {
                WorkoutExerciseId = parentExercise.WorkoutExerciseId,
                WorkoutSessionId = parentExercise.WorkoutSessionId,
                SetNumber = parentExercise.Sets.Count + 1,
                Weight = newWeight,
                Reps = newReps
            };

            await _dbContext.SaveSetAsync(newSet);
            await LoadHistoryAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteExerciseAsync(WorkoutHistoryExercise exercise)
    {
        if (exercise == null) return;
        bool confirm = await Shell.Current.DisplayAlertAsync(UiText.TitleDeleteExercise, string.Format(UiText.RemoveExerciseConfirmationFormat, exercise.ExerciseName), UiText.ButtonYes, UiText.ButtonNo);
        if (!confirm) return;

        await _dbContext.DeleteLoggedExerciseAsync(exercise.WorkoutExerciseId, exercise.WorkoutSessionId);
        await LoadHistoryAsync();
    }

    [RelayCommand]
    private async Task AddExerciseAsync(WorkoutHistorySession session)
    {
        if (session == null) return;
        
        var allExercises = await _dbContext.GetExercisesAsync();
        if (!allExercises.Any())
        {
            await Shell.Current.DisplayAlertAsync(UiText.TitleNoExercises, UiText.BodyNoExercisesInLibrary, UiText.ButtonOk);
            return;
        }

        var exerciseNames = allExercises.Select(e => e.Name).ToArray();
        string selectedName = await Shell.Current.DisplayActionSheetAsync(UiText.TitleSelectExercise, UiText.ButtonCancel, null, exerciseNames);

        if (string.IsNullOrEmpty(selectedName) || selectedName == UiText.ButtonCancel)
            return;

        var selectedExercise = allExercises.First(e => e.Name == selectedName);

        var newWorkoutExercise = new WorkoutExercise
        {
            WorkoutId = _workoutId,
            ExerciseId = selectedExercise.Id,
            ExerciseName = selectedExercise.Name,
            Sets = DomainDefaults.Sets,
            Reps = DomainDefaults.Reps,
            BreakTimeInSeconds = DomainDefaults.BreakTimeInSeconds,
            TargetRPE = DomainDefaults.TargetRPE
        };
        
        int workoutExerciseId = await _dbContext.AddLoggedExerciseAsync(newWorkoutExercise);
        
        var newSet = new WorkoutSet
        {
            WorkoutExerciseId = workoutExerciseId,
            WorkoutSessionId = session.SessionId,
            SetNumber = 1,
            Weight = 0,
            Reps = 0
        };
        await _dbContext.SaveSetAsync(newSet);

        await LoadHistoryAsync();
    }
}
