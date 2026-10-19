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
    private const string MeasuredDigit = "0";

    private static readonly float[] TrendDash = [4, 4];
    private static readonly Microsoft.Maui.Graphics.Font LabelFont = new(FontFamilies.BarlowSemiCondensedFile);

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

        var labelHeight = canvas.GetStringSize(MeasuredDigit, LabelFont, fontSize).Height;
        var labelWidth = model.YTicks.Max(tick => canvas.GetStringSize(tick.Label, LabelFont, fontSize).Width);
        var plot = new RectF(
            dirtyRect.Left + labelWidth + LabelGap,
            dirtyRect.Top + (labelHeight * Half),
            dirtyRect.Width - labelWidth - LabelGap - SelectedRadius,
            dirtyRect.Height - (labelHeight * Half) - labelHeight - LabelGap);
        var x = new LinearScale(model.FirstX, model.LastX, plot.Left + SelectedRadius, plot.Right - SelectedRadius);
        var y = new LinearScale(model.MinimumY, model.MaximumY, plot.Bottom, plot.Top);
        List<PointF> points = [.. model.Points.Select(point => new PointF(x.Map(point.X), y.Map(point.Y)))];

        DrawValueAxis(canvas, model, plot, y, fontSize);
        DrawDateAxis(canvas, model, dirtyRect, plot, x, fontSize);
        DrawLine(canvas, points);
        DrawMarkers(canvas, points, plot);
        DrawTrend(canvas, model, x, y);
        DrawSelection(canvas, points, plot);

        PointXs = [.. points.Select(point => point.X)];
    }

    private void DrawValueAxis(ICanvas canvas, ChartModel model, RectF plot, LinearScale y, float fontSize)
    {
        canvas.StrokeColor = chart.GridColor;
        canvas.StrokeSize = GridWidth;

        foreach (var tick in model.YTicks)
        {
            var tickY = y.Map(tick.Value);
            canvas.DrawLine(plot.Left, tickY, plot.Right, tickY);
            canvas.DrawString(tick.Label, plot.Left - LabelGap, tickY + (fontSize * BaselineToCenter), HorizontalAlignment.Right);
        }
    }

    private static void DrawDateAxis(ICanvas canvas, ChartModel model, RectF bounds, RectF plot, LinearScale x, float fontSize)
    {
        var baseline = plot.Bottom + LabelGap + fontSize;

        foreach (var tick in model.XTicks)
        {
            var halfWidth = canvas.GetStringSize(tick.Label, LabelFont, fontSize).Width * Half;
            var center = Math.Clamp(x.Map(tick.Value), plot.Left + halfWidth, bounds.Right - halfWidth);
            canvas.DrawString(tick.Label, center, baseline, HorizontalAlignment.Center);
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
