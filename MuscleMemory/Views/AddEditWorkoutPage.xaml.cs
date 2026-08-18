using MuscleMemory.ViewModels;

namespace MuscleMemory.Views;

public partial class AddEditWorkoutPage : ContentPage
{
    public AddEditWorkoutPage(AddEditWorkoutViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
