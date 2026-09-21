using System.Windows.Input;

namespace MuscleMemory.Controls;

public partial class InputStepper : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(InputStepper), string.Empty);

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(InputStepper), string.Empty, BindingMode.TwoWay);

    public static readonly BindableProperty DecrementCommandProperty =
        BindableProperty.Create(nameof(DecrementCommand), typeof(ICommand), typeof(InputStepper));

    public static readonly BindableProperty IncrementCommandProperty =
        BindableProperty.Create(nameof(IncrementCommand), typeof(ICommand), typeof(InputStepper));

    public InputStepper() => InitializeComponent();

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public ICommand? DecrementCommand
    {
        get => (ICommand?)GetValue(DecrementCommandProperty);
        set => SetValue(DecrementCommandProperty, value);
    }

    public ICommand? IncrementCommand
    {
        get => (ICommand?)GetValue(IncrementCommandProperty);
        set => SetValue(IncrementCommandProperty, value);
    }
}
