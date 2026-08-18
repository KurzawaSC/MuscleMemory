using MuscleMemory.ViewModels;

namespace MuscleMemory.Views;

public partial class WorkoutListPage : ContentPage
{
    public WorkoutListPage(WorkoutListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
