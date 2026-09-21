using MuscleMemory.Controls;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Views;

public partial class SettingsPage : BackNavigationPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}