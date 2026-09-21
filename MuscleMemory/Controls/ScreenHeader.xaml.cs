namespace MuscleMemory.Controls;

public partial class ScreenHeader : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(ScreenHeader), string.Empty);

    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(ScreenHeader), string.Empty,
            propertyChanged: (bindable, _, _) => ((ScreenHeader)bindable).OnPropertyChanged(nameof(HasSubtitle)));

    public ScreenHeader() => InitializeComponent();

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public bool HasSubtitle => !string.IsNullOrEmpty(Subtitle);
}
