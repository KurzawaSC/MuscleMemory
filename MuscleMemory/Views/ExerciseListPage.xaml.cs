using MuscleMemory.Controls;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Views;

public partial class ExerciseListPage : BackNavigationPage
{
    public ExerciseListPage(ExerciseListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
