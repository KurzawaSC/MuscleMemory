namespace MuscleMemory.Controls;

public partial class StatTile : Border
{
    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(object), typeof(StatTile));

    public static readonly BindableProperty ValueFormatProperty =
        BindableProperty.Create(nameof(ValueFormat), typeof(string), typeof(StatTile), "{0}");

    public static readonly BindableProperty UnitProperty =
        BindableProperty.Create(nameof(Unit), typeof(string), typeof(StatTile), string.Empty);

    public static readonly BindableProperty CaptionProperty =
        BindableProperty.Create(nameof(Caption), typeof(string), typeof(StatTile), string.Empty);

    public static readonly BindableProperty ContentSpacingProperty =
        BindableProperty.Create(nameof(ContentSpacing), typeof(double), typeof(StatTile), 0d);

    public static readonly BindableProperty DescriptionProperty =
        BindableProperty.Create(nameof(Description), typeof(string), typeof(StatTile), string.Empty, propertyChanged: OnDescriptionChanged);

    public StatTile() => InitializeComponent();

    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string ValueFormat
    {
        get => (string)GetValue(ValueFormatProperty);
        set => SetValue(ValueFormatProperty, value);
    }

    public string Unit
    {
        get => (string)GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public string Caption
    {
        get => (string)GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public double ContentSpacing
    {
        get => (double)GetValue(ContentSpacingProperty);
        set => SetValue(ContentSpacingProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    private static void OnDescriptionChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((StatTile)bindable).Describe((string)newValue);

    private void Describe(string description)
    {
        SemanticProperties.SetDescription(this, description);
        AutomationProperties.SetExcludedWithChildren(Values, !string.IsNullOrEmpty(description));
    }
}
