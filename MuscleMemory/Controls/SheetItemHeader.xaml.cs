namespace MuscleMemory.Controls;

public partial class SheetItemHeader : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(SheetItemHeader), string.Empty);

    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(SheetItemHeader), string.Empty,
            propertyChanged: (bindable, _, _) => ((SheetItemHeader)bindable).OnPropertyChanged(nameof(HasSubtitle)));

    public SheetItemHeader() => InitializeComponent();

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
