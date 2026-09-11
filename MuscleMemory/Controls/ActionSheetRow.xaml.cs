using System.Windows.Input;

namespace MuscleMemory.Controls;

public partial class ActionSheetRow : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(ActionSheetRow), string.Empty);

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(ActionSheetRow), Colors.Transparent);

    public static readonly BindableProperty ShowsChevronProperty =
        BindableProperty.Create(nameof(ShowsChevron), typeof(bool), typeof(ActionSheetRow), true);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(ActionSheetRow));

    public ActionSheetRow() => InitializeComponent();

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
