using MuscleMemory.ViewModels;

namespace MuscleMemory.Views;

public partial class ExerciseListPage : ContentPage
{
    public ExerciseListPage(ExerciseListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
