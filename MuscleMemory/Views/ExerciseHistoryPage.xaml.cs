namespace MuscleMemory.Views;

public partial class ExerciseHistoryPage : ContentPage
{
    public ExerciseHistoryPage(ViewModels.ExerciseHistoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
