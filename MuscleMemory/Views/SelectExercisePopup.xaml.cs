using CommunityToolkit.Maui.Views;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Views;

public partial class SelectExercisePopup : Popup
{
    public SelectExercisePopup(SelectExerciseViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
