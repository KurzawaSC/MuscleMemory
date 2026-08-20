using System.Windows.Input;

namespace MuscleMemory.Controls;

public partial class SettingsActionRow : ContentView
{
    public static readonly BindableProperty IconSourceProperty =
        BindableProperty.Create(nameof(IconSource), typeof(ImageSource), typeof(SettingsActionRow));

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(SettingsActionRow), string.Empty);

    public static readonly BindableProperty TintProperty =
        BindableProperty.Create(nameof(Tint), typeof(Color), typeof(SettingsActionRow), Colors.Transparent);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(SettingsActionRow));

    public SettingsActionRow() => InitializeComponent();

    public ImageSource? IconSource
    {
        get => (ImageSource?)GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Color Tint
    {
        get => (Color)GetValue(TintProperty);
        set => SetValue(TintProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
}
