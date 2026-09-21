namespace MuscleMemory.Controls;

public class RestRing : GraphicsView
{
    private const float DefaultThickness = 12f;
    private const string AnimationName = nameof(RestRing);
    private const uint TickMilliseconds = 1000;
    private const uint JumpMilliseconds = 250;
    private const double TickStepLimit = 0.2;

    public static readonly BindableProperty ProgressProperty =
        BindableProperty.Create(nameof(Progress), typeof(double), typeof(RestRing), 0d,
            propertyChanged: (bindable, oldValue, newValue) => ((RestRing)bindable).AnimateTo((double)oldValue, (double)newValue));

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

    internal double DisplayProgress { get; private set; }

    private void AnimateTo(double from, double to)
    {
        this.AbortAnimation(AnimationName);

        var isTick = to < from && from - to <= TickStepLimit;
        var animation = new Animation(value =>
        {
            DisplayProgress = value;
            Invalidate();
        }, DisplayProgress, to);

        animation.Commit(this, AnimationName,
            length: isTick ? TickMilliseconds : JumpMilliseconds,
            easing: isTick ? Easing.Linear : Easing.CubicOut);
    }
}
