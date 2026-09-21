using MuscleMemory.Controls;

namespace MuscleMemory.Views;

public partial class WorkoutHistoryPage : BackNavigationPage
{
    public WorkoutHistoryPage(ViewModels.WorkoutHistoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
