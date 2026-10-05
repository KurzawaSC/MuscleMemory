using MuscleMemory.Controls;

namespace MuscleMemory.Views;

public partial class ExerciseHistoryPage : BackNavigationPage
{
    public ExerciseHistoryPage(ViewModels.ExerciseHistoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
