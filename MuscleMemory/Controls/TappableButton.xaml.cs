using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MuscleMemory.Controls;

public partial class TappableButton : ContentView
{
    private const double PressedScale = 0.96;
    private const uint PressFeedbackMilliseconds = 60;

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(TappableButton), string.Empty);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(TappableButton));

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(TappableButton));

    public static readonly BindableProperty FillColorProperty =
        BindableProperty.Create(nameof(FillColor), typeof(Color), typeof(TappableButton), Colors.Transparent);

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(TappableButton), Colors.Transparent);

    public static readonly BindableProperty StrokeColorProperty =
        BindableProperty.Create(nameof(StrokeColor), typeof(Color), typeof(TappableButton), Colors.Transparent);

    public static readonly BindableProperty StrokeThicknessProperty =
        BindableProperty.Create(nameof(StrokeThickness), typeof(double), typeof(TappableButton), 0d);

    public static readonly BindableProperty CornerRadiusProperty =
        BindableProperty.Create(nameof(CornerRadius), typeof(CornerRadius), typeof(TappableButton), new CornerRadius(30));

    public static readonly BindableProperty FontSizeProperty =
        BindableProperty.Create(nameof(FontSize), typeof(double), typeof(TappableButton), 18d);

    public static readonly BindableProperty FontAttributesProperty =
        BindableProperty.Create(nameof(FontAttributes), typeof(FontAttributes), typeof(TappableButton), FontAttributes.None);

    public static readonly BindableProperty ContentPaddingProperty =
        BindableProperty.Create(nameof(ContentPadding), typeof(Thickness), typeof(TappableButton), new Thickness(14, 10));

    public static readonly BindableProperty SurfaceShadowProperty =
        BindableProperty.Create(nameof(SurfaceShadow), typeof(Shadow), typeof(TappableButton));

    public static readonly BindableProperty DisabledOpacityProperty =
        BindableProperty.Create(nameof(DisabledOpacity), typeof(double), typeof(TappableButton), 0.4d,
            propertyChanged: (bindable, _, _) => ((TappableButton)bindable).OnPropertyChanged(nameof(SurfaceOpacity)));

    public TappableButton() => InitializeComponent();

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public Color FillColor
    {
        get => (Color)GetValue(FillColorProperty);
        set => SetValue(FillColorProperty, value);
    }

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public Color StrokeColor
    {
        get => (Color)GetValue(StrokeColorProperty);
        set => SetValue(StrokeColorProperty, value);
    }

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontAttributes FontAttributes
    {
        get => (FontAttributes)GetValue(FontAttributesProperty);
        set => SetValue(FontAttributesProperty, value);
    }

    public Thickness ContentPadding
    {
        get => (Thickness)GetValue(ContentPaddingProperty);
        set => SetValue(ContentPaddingProperty, value);
    }

    public Shadow? SurfaceShadow
    {
        get => (Shadow?)GetValue(SurfaceShadowProperty);
        set => SetValue(SurfaceShadowProperty, value);
    }

    public double DisabledOpacity
    {
        get => (double)GetValue(DisabledOpacityProperty);
        set => SetValue(DisabledOpacityProperty, value);
    }

    public double SurfaceOpacity => IsEnabled ? 1 : DisabledOpacity;

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == IsEnabledProperty.PropertyName)
        {
            OnPropertyChanged(nameof(SurfaceOpacity));
        }
    }

    private async void OnTapped(object? sender, TappedEventArgs e)
    {
        if (!IsEnabled || Command?.CanExecute(CommandParameter) != true)
        {
            return;
        }

        await Surface.ScaleToAsync(PressedScale, PressFeedbackMilliseconds);
        await Surface.ScaleToAsync(1, PressFeedbackMilliseconds);

        Command.Execute(CommandParameter);
    }
}
