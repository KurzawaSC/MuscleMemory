using MuscleMemory.Controls;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Views;

public partial class WorkoutListPage : BackNavigationPage
{
    public WorkoutListPage(WorkoutListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
