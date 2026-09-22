namespace MuscleMemory.Controls;

internal sealed class RestRingDrawable(RestRing ring) : IDrawable
{
    private const float TrackAlpha = 0.25f;
    private const float TopAngle = 90;
    private const float FullTurn = 360;
    private const double CompleteThreshold = 0.999;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var inset = ring.Thickness / 2;
        var bounds = dirtyRect.Inflate(-inset, -inset);

        canvas.StrokeSize = ring.Thickness;
        canvas.StrokeLineCap = LineCap.Round;

        canvas.StrokeColor = ring.TrackColor.WithAlpha(TrackAlpha);
        canvas.DrawEllipse(bounds);

        if (ring.DisplayProgress <= 0)
        {
            return;
        }

        canvas.StrokeColor = ring.ProgressColor;

        if (ring.DisplayProgress >= CompleteThreshold)
        {
            canvas.DrawEllipse(bounds);
            return;
        }

        var endAngle = TopAngle - FullTurn * (float)ring.DisplayProgress;
        canvas.DrawArc(bounds.X, bounds.Y, bounds.Width, bounds.Height, TopAngle, endAngle, clockwise: true, closed: false);
    }
}
