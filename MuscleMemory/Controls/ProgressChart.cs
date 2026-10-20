using System.Windows.Input;
using MuscleMemory.Charting;

namespace MuscleMemory.Controls;

public class ProgressChart : GraphicsView
{
    public const float TouchTolerance = 24;
    public const int NoSelection = -1;

    public static readonly BindableProperty ModelProperty =
        BindableProperty.Create(nameof(Model), typeof(ChartModel), typeof(ProgressChart), null, propertyChanged: Redraw);

    public static readonly BindableProperty SelectedIndexProperty =
        BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(ProgressChart), NoSelection, propertyChanged: Redraw);

    public static readonly BindableProperty SelectCommandProperty =
        BindableProperty.Create(nameof(SelectCommand), typeof(ICommand), typeof(ProgressChart));

    public static readonly BindableProperty LineColorProperty =
        BindableProperty.Create(nameof(LineColor), typeof(Color), typeof(ProgressChart), Colors.Black, propertyChanged: Redraw);

    public static readonly BindableProperty SurfaceColorProperty =
        BindableProperty.Create(nameof(SurfaceColor), typeof(Color), typeof(ProgressChart), Colors.White, propertyChanged: Redraw);

    public static readonly BindableProperty GridColorProperty =
        BindableProperty.Create(nameof(GridColor), typeof(Color), typeof(ProgressChart), Colors.LightGray, propertyChanged: Redraw);

    public static readonly BindableProperty LabelColorProperty =
        BindableProperty.Create(nameof(LabelColor), typeof(Color), typeof(ProgressChart), Colors.Gray, propertyChanged: Redraw);

    public static readonly BindableProperty TrendColorProperty =
        BindableProperty.Create(nameof(TrendColor), typeof(Color), typeof(ProgressChart), Colors.Gray, propertyChanged: Redraw);

    public static readonly BindableProperty GuideColorProperty =
        BindableProperty.Create(nameof(GuideColor), typeof(Color), typeof(ProgressChart), Colors.Gray, propertyChanged: Redraw);

    private readonly ProgressChartDrawable _drawable;

    public ProgressChart()
    {
        Drawable = _drawable = new ProgressChartDrawable(this);
        StartInteraction += OnStartInteraction;
        DragInteraction += OnDragInteraction;
    }

    public ChartModel? Model
    {
        get => (ChartModel?)GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public ICommand? SelectCommand
    {
        get => (ICommand?)GetValue(SelectCommandProperty);
        set => SetValue(SelectCommandProperty, value);
    }

    public Color LineColor
    {
        get => (Color)GetValue(LineColorProperty);
        set => SetValue(LineColorProperty, value);
    }

    public Color SurfaceColor
    {
        get => (Color)GetValue(SurfaceColorProperty);
        set => SetValue(SurfaceColorProperty, value);
    }

    public Color GridColor
    {
        get => (Color)GetValue(GridColorProperty);
        set => SetValue(GridColorProperty, value);
    }

    public Color LabelColor
    {
        get => (Color)GetValue(LabelColorProperty);
        set => SetValue(LabelColorProperty, value);
    }

    public Color TrendColor
    {
        get => (Color)GetValue(TrendColorProperty);
        set => SetValue(TrendColorProperty, value);
    }

    public Color GuideColor
    {
        get => (Color)GetValue(GuideColorProperty);
        set => SetValue(GuideColorProperty, value);
    }

    private static void Redraw(BindableObject bindable, object oldValue, object newValue) => ((ProgressChart)bindable).Invalidate();

    private void OnStartInteraction(object? sender, TouchEventArgs e) => SelectNearest(e, clearWhenMissed: true);

    private void OnDragInteraction(object? sender, TouchEventArgs e) => SelectNearest(e, clearWhenMissed: false);

    private void SelectNearest(TouchEventArgs e, bool clearWhenMissed)
    {
        if (e.Touches.Length == 0)
        {
            return;
        }

        var nearest = ChartHitTest.Nearest(_drawable.PointXs, e.Touches[0].X, TouchTolerance);

        if ((nearest is null && !clearWhenMissed) || (nearest ?? NoSelection) == SelectedIndex)
        {
            return;
        }

        if (SelectCommand?.CanExecute(nearest) == true)
        {
            SelectCommand.Execute(nearest);
        }
    }
}
