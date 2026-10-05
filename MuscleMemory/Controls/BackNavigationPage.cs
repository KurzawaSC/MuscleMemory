using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace MuscleMemory.Controls;

public class BackNavigationPage : ContentPage
{
    public static readonly BindableProperty BackCommandProperty =
        BindableProperty.Create(nameof(BackCommand), typeof(ICommand), typeof(BackNavigationPage));

    public ICommand? BackCommand
    {
        get => (ICommand?)GetValue(BackCommandProperty);
        set => SetValue(BackCommandProperty, value);
    }

    protected override bool OnBackButtonPressed()
    {
        if (BackCommand is IAsyncRelayCommand { IsRunning: true })
        {
            return true;
        }

        if (BackCommand?.CanExecute(null) != true)
        {
            return base.OnBackButtonPressed();
        }

        BackCommand.Execute(null);
        return true;
    }
}
