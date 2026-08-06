using MuscleMemory.ViewModels;

namespace MuscleMemory.Views;

public partial class WorkoutListPage : ContentPage
{
    private readonly WorkoutListViewModel _viewModel;

    public WorkoutListPage(WorkoutListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadWorkoutsAsync();
    }
}