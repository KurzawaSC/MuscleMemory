using CommunityToolkit.Maui.Views;
using MuscleMemory.Models;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Views;
public partial class AddExercisePopup : Popup
{
    public Exercise? ReturnedExercise { get; private set; }
    private readonly AddEditExerciseViewModel _viewModel;

    public AddExercisePopup(Exercise? exerciseToEdit = null)
    {
        InitializeComponent();
        _viewModel = new AddEditExerciseViewModel();
        if (exerciseToEdit != null)
        {
            _viewModel.LoadExercise(exerciseToEdit);
        }
        BindingContext = _viewModel;
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        ReturnedExercise = null;
        await CloseAsync();
    }

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_viewModel.Name))
        {
            ReturnedExercise = _viewModel.GetExercise();
            await CloseAsync();
        }
    }
}