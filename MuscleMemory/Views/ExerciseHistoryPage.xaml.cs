using MuscleMemory.Controls;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Views;

public partial class ExerciseHistoryPage : BackNavigationPage
{
    public ExerciseHistoryPage(ExerciseHistoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
