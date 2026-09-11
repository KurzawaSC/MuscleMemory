using System.Windows.Input;

namespace MuscleMemory.Controls;

public partial class GlyphButton : ContentView
{
    private const double DefaultSurfaceSize = 36;

    public static readonly BindableProperty GlyphProperty =
        BindableProperty.Create(nameof(Glyph), typeof(string), typeof(GlyphButton), string.Empty);

    public static readonly BindableProperty GlyphColorProperty =
        BindableProperty.Create(nameof(GlyphColor), typeof(Color), typeof(GlyphButton), Colors.Transparent);

    public static readonly BindableProperty FillColorProperty =
        BindableProperty.Create(nameof(FillColor), typeof(Color), typeof(GlyphButton), Colors.Transparent);

    public static readonly BindableProperty StrokeColorProperty =
        BindableProperty.Create(nameof(StrokeColor), typeof(Color), typeof(GlyphButton), Colors.Transparent);

    public static readonly BindableProperty StrokeThicknessProperty =
        BindableProperty.Create(nameof(StrokeThickness), typeof(double), typeof(GlyphButton), 0d);

    public static readonly BindableProperty SurfaceSizeProperty =
        BindableProperty.Create(nameof(SurfaceSize), typeof(double), typeof(GlyphButton), DefaultSurfaceSize);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(GlyphButton));

    public GlyphButton() => InitializeComponent();

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public Color GlyphColor
    {
        get => (Color)GetValue(GlyphColorProperty);
        set => SetValue(GlyphColorProperty, value);
    }

    public Color FillColor
    {
        get => (Color)GetValue(FillColorProperty);
        set => SetValue(FillColorProperty, value);
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

    public double SurfaceSize
    {
        get => (double)GetValue(SurfaceSizeProperty);
        set => SetValue(SurfaceSizeProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
}
