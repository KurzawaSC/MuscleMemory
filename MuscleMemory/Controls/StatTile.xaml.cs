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
}
