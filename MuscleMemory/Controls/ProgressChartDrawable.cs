using MuscleMemory.Charting;
using MuscleMemory.Constants;

namespace MuscleMemory.Controls;

internal sealed class ProgressChartDrawable(ProgressChart chart) : IDrawable
{
    private const float LabelFontSize = 11;
    private const float LabelGap = 6;
    private const float LineWidth = 2.5f;
    private const float GridWidth = 1;
    private const float TrendWidth = 1.5f;
    private const float GuideWidth = 1;
    private const float MarkerRadius = 3;
    private const float MarkerRing = 1.5f;
    private const float SelectedRadius = 6;
    private const float SelectedRing = 2.5f;
    private const float MinMarkerSpacing = 8;
    private const float Half = 0.5f;
    private const float BaselineToCenter = 0.35f;

    private static readonly float[] TrendDash = [4, 4];
    private static readonly Microsoft.Maui.Graphics.Font LabelFont = new(FontFamilies.BarlowSemiCondensedFile);

    private ChartLabelSizes? _labels;

    public IReadOnlyList<float> PointXs { get; private set; } = [];

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (chart.Model is not { } model)
        {
            PointXs = [];
            return;
        }

        var fontSize = LabelFontSize * SystemFontScale.Current;
        canvas.Font = LabelFont;
        canvas.FontSize = fontSize;
        canvas.FontColor = chart.LabelColor;

        var labels = LabelSizes(canvas, model, fontSize);
        var plot = new RectF(
            dirtyRect.Left + labels.ValueWidth + LabelGap,
            dirtyRect.Top + (labels.Height * Half),
            dirtyRect.Width - labels.ValueWidth - LabelGap - SelectedRadius,
            dirtyRect.Height - (labels.Height * Half) - labels.Height - LabelGap);
        var x = new LinearScale(model.FirstX, model.LastX, plot.Left + SelectedRadius, plot.Right - SelectedRadius);
        var y = new LinearScale(model.MinimumY, model.MaximumY, plot.Bottom, plot.Top);
        List<PointF> points = [.. model.Points.Select(point => new PointF(x.Map(point.X), y.Map(point.Y)))];

        DrawValueAxis(canvas, model, labels, plot, y);
        DrawDateAxis(canvas, model, labels, dirtyRect, plot, x);
        DrawLine(canvas, points);
        DrawMarkers(canvas, points, plot);
        DrawTrend(canvas, model, x, y);
        DrawSelection(canvas, points, plot);

        PointXs = [.. points.Select(point => point.X)];
    }

    private ChartLabelSizes LabelSizes(ICanvas canvas, ChartModel model, float fontSize)
    {
        if (_labels is not { } labels || !labels.Fits(model, fontSize))
        {
            _labels = labels = ChartLabelSizes.Measure(canvas, model, LabelFont, fontSize);
        }

        return labels;
    }

    private void DrawValueAxis(ICanvas canvas, ChartModel model, ChartLabelSizes labels, RectF plot, LinearScale y)
    {
        canvas.StrokeColor = chart.GridColor;
        canvas.StrokeSize = GridWidth;

        foreach (var (tick, width) in model.YTicks.Zip(labels.ValueWidths))
        {
            var tickY = y.Map(tick.Value);
            canvas.DrawLine(plot.Left, tickY, plot.Right, tickY);
            canvas.DrawString(tick.Label, plot.Left - LabelGap - width, tickY + (labels.FontSize * BaselineToCenter), HorizontalAlignment.Left);
        }
    }

    private static void DrawDateAxis(ICanvas canvas, ChartModel model, ChartLabelSizes labels, RectF bounds, RectF plot, LinearScale x)
    {
        var baseline = plot.Bottom + LabelGap + labels.FontSize;

        foreach (var (tick, width) in model.XTicks.Zip(labels.DateWidths))
        {
            var halfWidth = width * Half;
            var center = Math.Clamp(x.Map(tick.Value), plot.Left + halfWidth, bounds.Right - halfWidth);
            canvas.DrawString(tick.Label, center - halfWidth, baseline, HorizontalAlignment.Left);
        }
    }

    private void DrawTrend(ICanvas canvas, ChartModel model, LinearScale x, LinearScale y)
    {
        if (model.TrendStart is not { } start || model.TrendEnd is not { } end)
        {
            return;
        }

        canvas.SaveState();
        canvas.StrokeColor = chart.TrendColor;
        canvas.StrokeSize = TrendWidth;
        canvas.StrokeDashPattern = TrendDash;
        canvas.DrawLine(x.Map(start.X), y.Map(start.Y), x.Map(end.X), y.Map(end.Y));
        canvas.RestoreState();
    }

    private void DrawLine(ICanvas canvas, List<PointF> points)
    {
        var path = new PathF(points[0]);

        foreach (var point in points.Skip(1))
        {
            path.LineTo(point);
        }

        canvas.StrokeColor = chart.LineColor;
        canvas.StrokeSize = LineWidth;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawPath(path);
    }

    private void DrawMarkers(ICanvas canvas, List<PointF> points, RectF plot)
    {
        if (points.Count > 1 && plot.Width / (points.Count - 1) < MinMarkerSpacing)
        {
            return;
        }

        foreach (var point in points)
        {
            DrawDot(canvas, point, MarkerRadius, MarkerRing);
        }
    }

    private void DrawSelection(ICanvas canvas, List<PointF> points, RectF plot)
    {
        if (chart.SelectedIndex < 0 || chart.SelectedIndex >= points.Count)
        {
            return;
        }

        var selected = points[chart.SelectedIndex];
        canvas.StrokeColor = chart.GuideColor;
        canvas.StrokeSize = GuideWidth;
        canvas.DrawLine(selected.X, plot.Top, selected.X, plot.Bottom);
        DrawDot(canvas, selected, SelectedRadius, SelectedRing);
    }

    private void DrawDot(ICanvas canvas, PointF center, float radius, float ring)
    {
        canvas.FillColor = chart.SurfaceColor;
        canvas.FillCircle(center, radius + ring);
        canvas.FillColor = chart.LineColor;
        canvas.FillCircle(center, radius);
    }
}
