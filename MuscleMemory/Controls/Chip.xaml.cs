namespace MuscleMemory.Controls;

public partial class Chip : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(Chip), string.Empty);

    public static readonly BindableProperty IsAccentProperty =
        BindableProperty.Create(nameof(IsAccent), typeof(bool), typeof(Chip), false);

    public Chip() => InitializeComponent();

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsAccent
    {
        get => (bool)GetValue(IsAccentProperty);
        set => SetValue(IsAccentProperty, value);
    }
}
