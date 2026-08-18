using MuscleMemory.Constants;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Views;

public partial class ActiveWorkoutPage : ContentPage
{
    public ActiveWorkoutPage(ActiveWorkoutViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override bool OnBackButtonPressed()
    {
        Shell.Current.GoToAsync(NavigationRoutes.GoBack);
        return true;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ActiveWorkoutViewModel vm)
            vm.IsOnActiveWorkoutPage = true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is ActiveWorkoutViewModel vm)
            vm.IsOnActiveWorkoutPage = false;
    }
}