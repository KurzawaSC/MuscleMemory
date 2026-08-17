using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using MuscleMemory.Data;
using MuscleMemory.Models;
using MuscleMemory.Views;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Extensions;

namespace MuscleMemory.ViewModels;

public partial class ExerciseListViewModel : ObservableObject
{
    private readonly DatabaseContext _dbContext;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

    [ObservableProperty]
    public partial bool IsNotEmpty { get; set; } = false;

    public ObservableCollection<Exercise> Exercises { get; set; } = new();

    public ExerciseListViewModel(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
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
    public async Task SaveNewExerciseAsync(Exercise newDoc)
    {
        await _dbContext.AddExerciseAsync(newDoc);
        await LoadExercisesAsync();
    }

    [RelayCommand]
    public async Task DeleteExerciseAsync(Exercise exercise)
    {
        if (exercise == null) return;
        
        bool answer = await Shell.Current.DisplayAlertAsync("Delete Exercise", $"Are you sure you want to delete '{exercise.Name}'?", "Yes", "No");
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
        
        var popup = new AddExercisePopup(exercise);
        var page = Shell.Current.CurrentPage;
        if (page != null)
        {
            await page.ShowPopupAsync(popup);
            var updatedExercise = popup.ReturnedExercise;
            if (updatedExercise != null)
            {
                await _dbContext.UpdateExerciseAsync(updatedExercise);
                await LoadExercisesAsync();
            }
        }
    }

    [RelayCommand]
    public async Task ViewHistoryAsync(Exercise exercise)
    {
        if (exercise == null) return;
        
        await Shell.Current.GoToAsync($"{nameof(ExerciseHistoryPage)}?ExerciseId={exercise.Id}&ExerciseName={exercise.Name}");
    }
}
