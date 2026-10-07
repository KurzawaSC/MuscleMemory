using System.Windows.Input;

namespace MuscleMemory.Controls;

public partial class ActionRow : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(ActionRow), string.Empty);

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(ActionRow), Colors.Transparent);

    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(string), typeof(ActionRow), string.Empty);

    public static readonly BindableProperty ShowsChevronProperty =
        BindableProperty.Create(nameof(ShowsChevron), typeof(bool), typeof(ActionRow), true);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(ActionRow));

    public ActionRow() => InitializeComponent();

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool ShowsChevron
    {
        get => (bool)GetValue(ShowsChevronProperty);
        set => SetValue(ShowsChevronProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
}
