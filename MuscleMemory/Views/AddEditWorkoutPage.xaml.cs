using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Views;

public partial class AddEditWorkoutPage : ContentPage
{
    private readonly AddEditWorkoutViewModel _viewModel;

    public AddEditWorkoutPage(AddEditWorkoutViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Shell.Current.Navigating += Shell_Navigating;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Shell.Current.Navigating -= Shell_Navigating;
    }

    private async void Shell_Navigating(object sender, ShellNavigatingEventArgs e)
    {
        if (e.Source == ShellNavigationSource.Pop || e.Source == ShellNavigationSource.PopToRoot)
        {
            if (_viewModel.HasUnsavedChanges)
            {
                e.Cancel();

                bool discard = await DisplayAlertAsync("Unsaved Changes", "You have unsaved changes. Are you sure you want to discard them and exit?", "Discard", "Cancel");
                if (discard)
                {
                    _viewModel.HasUnsavedChanges = false;
                    await Shell.Current.GoToAsync("..");
                }
            }
        }
    }
    private async void OnAddExerciseClicked(object sender, EventArgs e)
    {
        var allExercises = await _viewModel.GetAllExercisesAsync();

        if (!allExercises.Any())
        {
            await DisplayAlertAsync("Hold on!", "You don't have any exercises in the database. Go to the 'List' tab and add some first!", "OK");
            return;
        }
        var selectPopup = new SelectExercisePopup(allExercises);
        await this.ShowPopupAsync(selectPopup);

        var selectedExercise = selectPopup.SelectedExercise;
        if (selectedExercise == null) return;
        await Task.Delay(150);
        var configPopup = new ConfigureExercisePopup(selectedExercise);
        await this.ShowPopupAsync(configPopup);

        var configResult = configPopup.ReturnedConfig;

        if (configResult != null)
        {
            _viewModel.AddExerciseToWorkout(
                selectedExercise,
                configResult.Sets,
                configResult.Reps,
                configResult.BreakTime);
        }
    }

    private async void OnEditExerciseTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is Models.WorkoutExercise exerciseToEdit)
        {
            await OpenEditPopup(exerciseToEdit);
        }
    }

    private async void OnEditButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Models.WorkoutExercise exerciseToEdit)
        {
            await OpenEditPopup(exerciseToEdit);
        }
    }

    private async Task OpenEditPopup(Models.WorkoutExercise exerciseToEdit)
    {
        var configPopup = new ConfigureExercisePopup(exerciseToEdit);
        await this.ShowPopupAsync(configPopup);

        var configResult = configPopup.ReturnedConfig;
        if (configResult != null)
        {
            _viewModel.UpdateExerciseInWorkout(
                exerciseToEdit,
                configResult.Sets,
                configResult.Reps,
                configResult.BreakTime);
        }
    }
}
