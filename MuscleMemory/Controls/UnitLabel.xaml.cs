using System.Globalization;

namespace MuscleMemory.Controls;

public partial class UnitLabel : Label
{
    private const string ValueWithUnitKey = "ValueWithUnit";

    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(object), typeof(UnitLabel),
            propertyChanged: (bindable, _, _) => ((UnitLabel)bindable).OnPropertyChanged(nameof(DisplayValue)));

    public static readonly BindableProperty ValueFormatProperty =
        BindableProperty.Create(nameof(ValueFormat), typeof(string), typeof(UnitLabel), "{0}",
            propertyChanged: (bindable, _, _) => ((UnitLabel)bindable).OnPropertyChanged(nameof(DisplayValue)));

    public static readonly BindableProperty UnitProperty =
        BindableProperty.Create(nameof(Unit), typeof(string), typeof(UnitLabel), string.Empty,
            propertyChanged: (bindable, _, _) => ((UnitLabel)bindable).ApplyUnit());

    public static readonly BindableProperty ValueSpanStyleProperty =
        BindableProperty.Create(nameof(ValueSpanStyle), typeof(Style), typeof(UnitLabel));

    public static readonly BindableProperty ValueColorProperty =
        BindableProperty.Create(nameof(ValueColor), typeof(Color), typeof(UnitLabel));

    public UnitLabel() => InitializeComponent();

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

    public Style? ValueSpanStyle
    {
        get => (Style?)GetValue(ValueSpanStyleProperty);
        set => SetValue(ValueSpanStyleProperty, value);
    }

    public Color? ValueColor
    {
        get => (Color?)GetValue(ValueColorProperty);
        set => SetValue(ValueColorProperty, value);
    }

    public string DisplayValue => string.Format(CultureInfo.CurrentCulture, ValueFormat, Value);

    private void ApplyUnit() =>
        FormattedText = string.IsNullOrEmpty(Unit) ? null : (FormattedString)Resources[ValueWithUnitKey];
}
