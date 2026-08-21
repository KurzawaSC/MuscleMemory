using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using MuscleMemory.Constants;
using MuscleMemory.Data.Repositories;
using MuscleMemory.Extensions;
using MuscleMemory.Models;
using MuscleMemory.Views;

namespace MuscleMemory.ViewModels;

public partial class ExerciseListViewModel(IExerciseRepository exerciseRepository, ActiveWorkoutViewModel activeWorkout, IPopupService popupService) : ObservableObject
{
    private readonly IExerciseRepository _exerciseRepository = exerciseRepository;
    private readonly IPopupService _popupService = popupService;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

    public ObservableCollection<Exercise> Exercises { get; } = [];

    public ActiveWorkoutViewModel ActiveWorkout { get; } = activeWorkout;

    [RelayCommand]
    private async Task LoadExercisesAsync()
    {
        Exercises.ReplaceAll(await _exerciseRepository.GetAllAsync());
        IsEmpty = !Exercises.Any();
    }

    [RelayCommand]
    private async Task AddExerciseAsync()
    {
        var newExercise = await ShowExercisePopupAsync(null);
        if (newExercise != null)
        {
            await _exerciseRepository.AddAsync(newExercise);
            await LoadExercisesAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteExerciseAsync(Exercise exercise)
    {
        if (exercise == null) return;
        
        bool answer = await Shell.Current.DisplayAlertAsync(UiText.TitleDeleteExercise, string.Format(UiText.DeleteConfirmationFormat, exercise.Name), UiText.ButtonYes, UiText.ButtonNo);
        if (answer)
        {
            await _exerciseRepository.DeleteAsync(exercise.Id);
            await LoadExercisesAsync();
        }
    }

    [RelayCommand]
    private async Task EditExerciseAsync(Exercise exercise)
    {
        if (exercise == null) return;
        
        var updatedExercise = await ShowExercisePopupAsync(exercise);
        if (updatedExercise != null)
        {
            await _exerciseRepository.UpdateAsync(updatedExercise);
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
    private async Task ViewHistoryAsync(Exercise exercise)
    {
        if (exercise == null) return;
        
        var navigationParameter = new Dictionary<string, object>
        {
            { QueryKeys.ExerciseId, exercise.Id },
            { QueryKeys.ExerciseName, exercise.Name }
        };
        await Shell.Current.GoToAsync(nameof(ExerciseHistoryPage), navigationParameter);
    }
}
