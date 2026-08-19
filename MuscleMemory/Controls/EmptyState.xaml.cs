namespace MuscleMemory.Controls;

public partial class EmptyState : ContentView
{
    public static readonly BindableProperty IconSourceProperty =
        BindableProperty.Create(nameof(IconSource), typeof(ImageSource), typeof(EmptyState),
            propertyChanged: (bindable, _, _) => ((EmptyState)bindable).OnPropertyChanged(nameof(HasIcon)));

    public static readonly BindableProperty MessageProperty =
        BindableProperty.Create(nameof(Message), typeof(string), typeof(EmptyState), string.Empty);

    public static readonly BindableProperty DetailProperty =
        BindableProperty.Create(nameof(Detail), typeof(string), typeof(EmptyState), string.Empty,
            propertyChanged: (bindable, _, _) => ((EmptyState)bindable).OnPropertyChanged(nameof(HasDetail)));

    public EmptyState() => InitializeComponent();

    public ImageSource? IconSource
    {
        get => (ImageSource?)GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string Detail
    {
        get => (string)GetValue(DetailProperty);
        set => SetValue(DetailProperty, value);
    }

    public bool HasIcon => IconSource is not null;

    public bool HasDetail => !string.IsNullOrEmpty(Detail);
}
