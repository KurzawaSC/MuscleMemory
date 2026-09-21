namespace MuscleMemory.Controls;

public class RestRing : GraphicsView
{
    private const float DefaultThickness = 12f;

    public static readonly BindableProperty ProgressProperty =
        BindableProperty.Create(nameof(Progress), typeof(double), typeof(RestRing), 0d,
            propertyChanged: (bindable, _, _) => ((RestRing)bindable).Invalidate());

    public static readonly BindableProperty ProgressColorProperty =
        BindableProperty.Create(nameof(ProgressColor), typeof(Color), typeof(RestRing), Colors.White,
            propertyChanged: (bindable, _, _) => ((RestRing)bindable).Invalidate());

    public static readonly BindableProperty TrackColorProperty =
        BindableProperty.Create(nameof(TrackColor), typeof(Color), typeof(RestRing), Colors.White,
            propertyChanged: (bindable, _, _) => ((RestRing)bindable).Invalidate());

    public static readonly BindableProperty ThicknessProperty =
        BindableProperty.Create(nameof(Thickness), typeof(float), typeof(RestRing), DefaultThickness,
            propertyChanged: (bindable, _, _) => ((RestRing)bindable).Invalidate());

    public RestRing() => Drawable = new RestRingDrawable(this);

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public Color ProgressColor
    {
        get => (Color)GetValue(ProgressColorProperty);
        set => SetValue(ProgressColorProperty, value);
    }

    public Color TrackColor
    {
        get => (Color)GetValue(TrackColorProperty);
        set => SetValue(TrackColorProperty, value);
    }

    public float Thickness
    {
        get => (float)GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }
}
