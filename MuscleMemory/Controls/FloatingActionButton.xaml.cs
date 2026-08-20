using System.Windows.Input;

namespace MuscleMemory.Controls;

public partial class FloatingActionButton : ContentView
{
    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(FloatingActionButton));

    public FloatingActionButton() => InitializeComponent();

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
}
