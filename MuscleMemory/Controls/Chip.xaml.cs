namespace MuscleMemory.Controls;

public partial class Chip : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(Chip), string.Empty);

    public Chip() => InitializeComponent();

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}
