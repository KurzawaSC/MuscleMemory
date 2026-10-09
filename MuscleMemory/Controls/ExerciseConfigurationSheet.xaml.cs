using System.Windows.Input;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Controls;

public partial class ExerciseConfigurationSheet : ContentView
{
    public static readonly BindableProperty ConfigurationProperty =
        BindableProperty.Create(nameof(Configuration), typeof(ExerciseConfigurationViewModel), typeof(ExerciseConfigurationSheet));

    public static readonly BindableProperty RemoveCommandProperty =
        BindableProperty.Create(nameof(RemoveCommand), typeof(ICommand), typeof(ExerciseConfigurationSheet));

    public ExerciseConfigurationSheet() => InitializeComponent();

    public ExerciseConfigurationViewModel? Configuration
    {
        get => (ExerciseConfigurationViewModel?)GetValue(ConfigurationProperty);
        set => SetValue(ConfigurationProperty, value);
    }

    public ICommand? RemoveCommand
    {
        get => (ICommand?)GetValue(RemoveCommandProperty);
        set => SetValue(RemoveCommandProperty, value);
    }
}
