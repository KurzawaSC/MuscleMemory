using CommunityToolkit.Maui.Views;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Views;

public partial class ConfigureExercisePopup : Popup
{
    public ConfigureExercisePopup(ConfigureExerciseViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
