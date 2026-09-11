using MuscleMemory.Controls;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Views;

public partial class AddEditWorkoutPage : BackNavigationPage
{
    public AddEditWorkoutPage(AddEditWorkoutViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
