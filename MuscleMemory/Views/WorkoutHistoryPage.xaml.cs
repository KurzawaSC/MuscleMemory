namespace MuscleMemory.Views;

public partial class WorkoutHistoryPage : ContentPage
{
    public WorkoutHistoryPage(ViewModels.WorkoutHistoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
