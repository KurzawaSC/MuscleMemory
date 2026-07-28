using MuscleMemory.ViewModels;

namespace MuscleMemory.Views;

public partial class ActiveWorkoutPage : ContentPage
{
    public ActiveWorkoutPage(ActiveWorkoutViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}