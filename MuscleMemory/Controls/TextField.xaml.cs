namespace MuscleMemory.Controls;

public partial class TextField : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(TextField), string.Empty, BindingMode.TwoWay);

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(TextField), string.Empty);

    public static readonly BindableProperty ClearButtonVisibilityProperty =
        BindableProperty.Create(nameof(ClearButtonVisibility), typeof(ClearButtonVisibility), typeof(TextField), ClearButtonVisibility.Never);

    public static readonly BindableProperty IsFilledProperty =
        BindableProperty.Create(nameof(IsFilled), typeof(bool), typeof(TextField), false);

    public TextField() => InitializeComponent();

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public ClearButtonVisibility ClearButtonVisibility
    {
        get => (ClearButtonVisibility)GetValue(ClearButtonVisibilityProperty);
        set => SetValue(ClearButtonVisibilityProperty, value);
    }

    public bool IsFilled
    {
        get => (bool)GetValue(IsFilledProperty);
        set => SetValue(IsFilledProperty, value);
    }
}
