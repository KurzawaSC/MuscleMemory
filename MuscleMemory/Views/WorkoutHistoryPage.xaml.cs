using MuscleMemory.Controls;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Views;

public partial class WorkoutHistoryPage : BackNavigationPage
{
    public WorkoutHistoryPage(WorkoutHistoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
