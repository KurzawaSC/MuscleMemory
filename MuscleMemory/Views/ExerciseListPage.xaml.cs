using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Views;

public partial class ExerciseListPage : ContentPage
{
    private readonly ExerciseListViewModel _viewModel;

    public ExerciseListPage(ExerciseListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadExercisesAsync();
    }

    // Zmieniono EventArgs na TappedEventArgs
    private async void OnAddButtonClicked(object sender, EventArgs e)
    {
        var popup = new AddExercisePopup();

        var result = await this.ShowPopupAsync(popup);

        string? newExerciseName = popup.ReturnedExerciseName;

        if (!string.IsNullOrWhiteSpace(newExerciseName))
        {
            await _viewModel.SaveNewExerciseAsync(newExerciseName);
        }
    }
}