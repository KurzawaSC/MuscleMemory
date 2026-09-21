using System.Windows.Input;

namespace MuscleMemory.Controls;

public partial class ValueStepper : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(ValueStepper), string.Empty);

    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(int), typeof(ValueStepper), 0, BindingMode.TwoWay,
            propertyChanged: (bindable, _, _) => ((ValueStepper)bindable).OnValueStateChanged());

    public static readonly BindableProperty MinimumProperty =
        BindableProperty.Create(nameof(Minimum), typeof(int), typeof(ValueStepper), 0,
            propertyChanged: (bindable, _, _) => ((ValueStepper)bindable).OnValueStateChanged());

    public static readonly BindableProperty MaximumProperty =
        BindableProperty.Create(nameof(Maximum), typeof(int), typeof(ValueStepper), int.MaxValue,
            propertyChanged: (bindable, _, _) => ((ValueStepper)bindable).OnValueStateChanged());

    public static readonly BindableProperty StepProperty =
        BindableProperty.Create(nameof(Step), typeof(int), typeof(ValueStepper), 1);

    public static readonly BindableProperty ValueFormatProperty =
        BindableProperty.Create(nameof(ValueFormat), typeof(string), typeof(ValueStepper), "{0}",
            propertyChanged: (bindable, _, _) => ((ValueStepper)bindable).OnValueStateChanged());

    public ValueStepper()
    {
        DecrementCommand = new Command(() => Value = Math.Max(Minimum, Value - Step));
        IncrementCommand = new Command(() => Value = Math.Min(Maximum, Value + Step));
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int Minimum
    {
        get => (int)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public int Maximum
    {
        get => (int)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public int Step
    {
        get => (int)GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    public string ValueFormat
    {
        get => (string)GetValue(ValueFormatProperty);
        set => SetValue(ValueFormatProperty, value);
    }

    public ICommand DecrementCommand { get; }

    public ICommand IncrementCommand { get; }

    public bool CanDecrement => Value > Minimum;

    public bool CanIncrement => Value < Maximum;

    public string DisplayValue => string.Format(ValueFormat, Value);

    private void OnValueStateChanged()
    {
        OnPropertyChanged(nameof(CanDecrement));
        OnPropertyChanged(nameof(CanIncrement));
        OnPropertyChanged(nameof(DisplayValue));
    }
}
