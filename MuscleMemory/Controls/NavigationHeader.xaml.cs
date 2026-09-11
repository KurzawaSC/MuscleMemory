using System.Windows.Input;

namespace MuscleMemory.Controls;

public partial class NavigationHeader : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(NavigationHeader), string.Empty);

    public static readonly BindableProperty BackCommandProperty =
        BindableProperty.Create(nameof(BackCommand), typeof(ICommand), typeof(NavigationHeader));

    public NavigationHeader() => InitializeComponent();

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public ICommand? BackCommand
    {
        get => (ICommand?)GetValue(BackCommandProperty);
        set => SetValue(BackCommandProperty, value);
    }
}
