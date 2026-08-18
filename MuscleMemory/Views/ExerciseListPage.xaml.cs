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

    private async void OnAddButtonClicked(object? sender, EventArgs e)
    {
        var popup = new AddExercisePopup();

        await this.ShowPopupAsync(popup);

        var newExercise = popup.ReturnedExercise;

        if (newExercise != null)
        {
            await _viewModel.SaveNewExerciseAsync(newExercise);
        }
    }
}
