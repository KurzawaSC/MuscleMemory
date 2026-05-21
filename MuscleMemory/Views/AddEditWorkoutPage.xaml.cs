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

    // Usunięto znak zapytania przy 'object sender' - to usatysfakcjonuje plik XAML
    private async void OnAddExerciseClicked(object sender, EventArgs e)
    {
        var allExercises = await _viewModel.GetAllExercisesAsync();

        if (!allExercises.Any())
        {
            await DisplayAlert("Hold on!", "You don't have any exercises in the database. Go to the 'List' tab and add some first!", "OK");
            return;
        }

        // --- OKNO 1: Wybór ---
        var selectPopup = new SelectExercisePopup(allExercises);
        await this.ShowPopupAsync(selectPopup);

        var selectedExercise = selectPopup.SelectedExercise;
        if (selectedExercise == null) return;

        // DODANA LINIA: Krótka przerwa dla Androida na posprzątanie animacji pierwszego okienka
        await Task.Delay(150);

        // --- OKNO 2: Konfiguracja ---
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
}