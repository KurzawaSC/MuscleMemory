namespace MuscleMemory.Controls;

public partial class TextField : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(TextField), string.Empty, BindingMode.TwoWay,
            propertyChanged: (bindable, _, _) => ((TextField)bindable).OnPropertyChanged(nameof(ShowsClearButton)));

    public static readonly BindableProperty MaxLengthProperty =
        BindableProperty.Create(nameof(MaxLength), typeof(int), typeof(TextField), int.MaxValue);

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(TextField), string.Empty);

    public static readonly BindableProperty IsClearableProperty =
        BindableProperty.Create(nameof(IsClearable), typeof(bool), typeof(TextField), false,
            propertyChanged: (bindable, _, _) => ((TextField)bindable).OnPropertyChanged(nameof(ShowsClearButton)));

    public static readonly BindableProperty IsFilledProperty =
        BindableProperty.Create(nameof(IsFilled), typeof(bool), typeof(TextField), false);

    public static readonly BindableProperty FieldHeightProperty =
        BindableProperty.Create(nameof(FieldHeight), typeof(double), typeof(TextField), HeightRequestProperty.DefaultValue);

    public static readonly BindableProperty ErrorTextProperty =
        BindableProperty.Create(nameof(ErrorText), typeof(string), typeof(TextField), string.Empty,
            propertyChanged: (bindable, _, _) => ((TextField)bindable).OnPropertyChanged(nameof(HasError)));

    public TextField() => InitializeComponent();

    public int MaxLength
    {
        get => (int)GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

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

    public bool IsClearable
    {
        get => (bool)GetValue(IsClearableProperty);
        set => SetValue(IsClearableProperty, value);
    }

    public bool IsFilled
    {
        get => (bool)GetValue(IsFilledProperty);
        set => SetValue(IsFilledProperty, value);
    }

    public double FieldHeight
    {
        get => (double)GetValue(FieldHeightProperty);
        set => SetValue(FieldHeightProperty, value);
    }

    public string ErrorText
    {
        get => (string)GetValue(ErrorTextProperty);
        set => SetValue(ErrorTextProperty, value);
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorText);

    public bool ShowsClearButton => IsClearable && !string.IsNullOrEmpty(Text);

    private void OnClearTapped(object? sender, TappedEventArgs e) => Text = string.Empty;
}
