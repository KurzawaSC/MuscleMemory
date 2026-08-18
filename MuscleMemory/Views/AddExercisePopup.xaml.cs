using CommunityToolkit.Maui.Views;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Views;

public partial class AddExercisePopup : Popup
{
    public AddExercisePopup(AddEditExerciseViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
