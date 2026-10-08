using System.Runtime.CompilerServices;
using System.Windows.Input;
using MuscleMemory.Constants;
using MuscleMemory.Diagnostics;

namespace MuscleMemory.Controls;

public partial class TappableButton : ContentView
{
    private const double DefaultCornerRadius = 30;
    private const double DefaultFontSize = 18;
    private const double DefaultHorizontalPadding = 14;
    private const double DefaultVerticalPadding = 10;
    private const double DefaultPressedScale = 0.98;
    private const double FillingSurfaceHeight = -1;
    private const uint PressPhaseMilliseconds = 45;

    private bool _isPressed;

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(TappableButton), string.Empty);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(TappableButton));

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(TappableButton));

    public static readonly BindableProperty FillColorProperty =
        BindableProperty.Create(nameof(FillColor), typeof(Color), typeof(TappableButton), Colors.Transparent,
            propertyChanged: (bindable, _, _) => ((TappableButton)bindable).OnPropertyChanged(nameof(SurfaceFill)));

    public static readonly BindableProperty PressedFillColorProperty =
        BindableProperty.Create(nameof(PressedFillColor), typeof(Color), typeof(TappableButton),
            propertyChanged: (bindable, _, _) => ((TappableButton)bindable).OnPropertyChanged(nameof(SurfaceFill)));

    public static readonly BindableProperty PressedScaleProperty =
        BindableProperty.Create(nameof(PressedScale), typeof(double), typeof(TappableButton), DefaultPressedScale);

    public static readonly BindableProperty TextColorProperty =
        BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(TappableButton), Colors.Transparent);

    public static readonly BindableProperty StrokeColorProperty =
        BindableProperty.Create(nameof(StrokeColor), typeof(Color), typeof(TappableButton), Colors.Transparent);

    public static readonly BindableProperty StrokeThicknessProperty =
        BindableProperty.Create(nameof(StrokeThickness), typeof(double), typeof(TappableButton), 0d);

    public static readonly BindableProperty CornerRadiusProperty =
        BindableProperty.Create(nameof(CornerRadius), typeof(CornerRadius), typeof(TappableButton), new CornerRadius(DefaultCornerRadius));

    public static readonly BindableProperty FontSizeProperty =
        BindableProperty.Create(nameof(FontSize), typeof(double), typeof(TappableButton), DefaultFontSize);

    public static readonly BindableProperty ContentPaddingProperty =
        BindableProperty.Create(nameof(ContentPadding), typeof(Thickness), typeof(TappableButton), new Thickness(DefaultHorizontalPadding, DefaultVerticalPadding));

    public static readonly BindableProperty SurfaceHeightProperty =
        BindableProperty.Create(nameof(SurfaceHeight), typeof(double), typeof(TappableButton), FillingSurfaceHeight,
            propertyChanged: (bindable, _, _) => ((TappableButton)bindable).OnPropertyChanged(nameof(SurfaceVerticalOptions)));

    public static readonly BindableProperty DisabledOpacityProperty =
        BindableProperty.Create(nameof(DisabledOpacity), typeof(double), typeof(TappableButton), Opacities.Disabled,
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

    public Color? PressedFillColor
    {
        get => (Color?)GetValue(PressedFillColorProperty);
        set => SetValue(PressedFillColorProperty, value);
    }

    public double PressedScale
    {
        get => (double)GetValue(PressedScaleProperty);
        set => SetValue(PressedScaleProperty, value);
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

    public Thickness ContentPadding
    {
        get => (Thickness)GetValue(ContentPaddingProperty);
        set => SetValue(ContentPaddingProperty, value);
    }

    public double SurfaceHeight
    {
        get => (double)GetValue(SurfaceHeightProperty);
        set => SetValue(SurfaceHeightProperty, value);
    }

    public double DisabledOpacity
    {
        get => (double)GetValue(DisabledOpacityProperty);
        set => SetValue(DisabledOpacityProperty, value);
    }

    public LayoutOptions SurfaceVerticalOptions => SurfaceHeight > 0 ? LayoutOptions.Center : LayoutOptions.Fill;

    public double SurfaceOpacity => IsEnabled ? 1 : DisabledOpacity;

    public Color SurfaceFill => _isPressed && PressedFillColor is { } pressedFill ? pressedFill : FillColor;

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == IsEnabledProperty.PropertyName)
        {
            OnPropertyChanged(nameof(SurfaceOpacity));
        }
    }

    private async void OnTapped(object? sender, TappedEventArgs e) =>
        await AppLog.LogFailuresAsync(PressAsync());

    private async Task PressAsync()
    {
        if (!CanRunCommand())
        {
            return;
        }

        SetPressed(true);
        await AppLog.LogFailuresAsync(AnimatePressAsync());
        SetPressed(false);

        if (CanRunCommand())
        {
            Command?.Execute(CommandParameter);
        }
    }

    private async Task AnimatePressAsync()
    {
        await Surface.ScaleToAsync(PressedScale, PressPhaseMilliseconds);
        await Surface.ScaleToAsync(1, PressPhaseMilliseconds);
    }

    private void SetPressed(bool isPressed)
    {
        _isPressed = isPressed;
        OnPropertyChanged(nameof(SurfaceFill));
    }

    private bool CanRunCommand() => IsEnabled && Command?.CanExecute(CommandParameter) == true;
}
