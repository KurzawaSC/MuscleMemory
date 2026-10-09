using System.Collections;
using System.Windows.Input;

namespace MuscleMemory.Controls;

public partial class LevelBar : ContentView
{
    private const string DisplaySegmentKey = "DisplaySegment";
    private const string TappableSegmentKey = "TappableSegment";

    public static readonly BindableProperty SegmentsProperty =
        BindableProperty.Create(nameof(Segments), typeof(IEnumerable), typeof(LevelBar));

    public static readonly BindableProperty SegmentHeightProperty =
        BindableProperty.Create(nameof(SegmentHeight), typeof(double), typeof(LevelBar), 0d);

    public static readonly BindableProperty SegmentRadiusProperty =
        BindableProperty.Create(nameof(SegmentRadius), typeof(CornerRadius), typeof(LevelBar), default(CornerRadius));

    public static readonly BindableProperty SegmentInsetProperty =
        BindableProperty.Create(nameof(SegmentInset), typeof(Thickness), typeof(LevelBar), default(Thickness));

    public static readonly BindableProperty SelectCommandProperty =
        BindableProperty.Create(nameof(SelectCommand), typeof(ICommand), typeof(LevelBar),
            propertyChanged: (bindable, _, _) => ((LevelBar)bindable).ApplySegmentTemplate());

    public LevelBar() => InitializeComponent();

    public IEnumerable? Segments
    {
        get => (IEnumerable?)GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    public double SegmentHeight
    {
        get => (double)GetValue(SegmentHeightProperty);
        set => SetValue(SegmentHeightProperty, value);
    }

    public CornerRadius SegmentRadius
    {
        get => (CornerRadius)GetValue(SegmentRadiusProperty);
        set => SetValue(SegmentRadiusProperty, value);
    }

    public Thickness SegmentInset
    {
        get => (Thickness)GetValue(SegmentInsetProperty);
        set => SetValue(SegmentInsetProperty, value);
    }

    public ICommand? SelectCommand
    {
        get => (ICommand?)GetValue(SelectCommandProperty);
        set => SetValue(SelectCommandProperty, value);
    }

    private void ApplySegmentTemplate()
    {
        var key = SelectCommand is null ? DisplaySegmentKey : TappableSegmentKey;
        BindableLayout.SetItemTemplate(SegmentLayout, (DataTemplate)Resources[key]);
    }
}
