using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using MuscleMemory.Constants;
using MuscleMemory.Data;
using MuscleMemory.Models;
using MuscleMemory.Views;

namespace MuscleMemory.ViewModels;

public partial class ExerciseListViewModel : ObservableObject
{
    private readonly DatabaseContext _dbContext;
    private readonly ActiveWorkoutViewModel _activeWorkoutViewModel;
    private readonly IPopupService _popupService;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

    public bool CanAddItems => !(_activeWorkoutViewModel?.IsWorkoutActive ?? false);

    public ObservableCollection<Exercise> Exercises { get; set; } = new();

    public ExerciseListViewModel(DatabaseContext dbContext, ActiveWorkoutViewModel activeWorkoutViewModel, IPopupService popupService)
    {
        _dbContext = dbContext;
        _activeWorkoutViewModel = activeWorkoutViewModel;
        _popupService = popupService;
        
        _activeWorkoutViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ActiveWorkoutViewModel.IsWorkoutActive))
            {
                OnPropertyChanged(nameof(CanAddItems));
            }
        };
    }

    [RelayCommand]
    public async Task LoadExercisesAsync()
    {
        var exercisesFromDb = await _dbContext.GetExercisesAsync();
        Exercises.Clear();
        foreach (var exercise in exercisesFromDb)
        {
            Exercises.Add(exercise);
        }
        IsEmpty = !Exercises.Any();
    }

    [RelayCommand]
    private async Task AddExerciseAsync()
    {
        var newExercise = await ShowExercisePopupAsync(null);
        if (newExercise != null)
        {
            await _dbContext.AddExerciseAsync(newExercise);
            await LoadExercisesAsync();
        }
    }

    [RelayCommand]
    public async Task DeleteExerciseAsync(Exercise exercise)
    {
        if (exercise == null) return;
        
        bool answer = await Shell.Current.DisplayAlertAsync(UiText.TitleDeleteExercise, string.Format(UiText.DeleteConfirmationFormat, exercise.Name), UiText.ButtonYes, UiText.ButtonNo);
        if (answer)
        {
            await _dbContext.DeleteExerciseAsync(exercise.Id);
            await LoadExercisesAsync();
        }
    }

    [RelayCommand]
    public async Task EditExerciseAsync(Exercise exercise)
    {
        if (exercise == null) return;
        
        var updatedExercise = await ShowExercisePopupAsync(exercise);
        if (updatedExercise != null)
        {
            await _dbContext.UpdateExerciseAsync(updatedExercise);
            await LoadExercisesAsync();
        }
    }

    private async Task<Exercise?> ShowExercisePopupAsync(Exercise? exerciseToEdit)
    {
        var shellParameters = exerciseToEdit == null
            ? null
            : new Dictionary<string, object> { [QueryKeys.ExerciseToEdit] = exerciseToEdit };

        var result = await _popupService.ShowPopupAsync<AddExercisePopup, Exercise?>(Shell.Current, shellParameters: shellParameters);

        return result.Result;
    }

    [RelayCommand]
    public async Task ViewHistoryAsync(Exercise exercise)
    {
        if (exercise == null) return;
        
        await Shell.Current.GoToAsync($"{nameof(ExerciseHistoryPage)}?{QueryKeys.ExerciseId}={exercise.Id}&{QueryKeys.ExerciseName}={exercise.Name}");
    }
}
