namespace MuscleMemory.Controls;

internal sealed record FocusedFieldScroll(
    bool IsAttached,
    double ContentHeight,
    double ViewportHeight,
    double ScrollY,
    double FieldTop,
    double FieldHeight)
{
    private const double Tolerance = 0.5;

    private double FieldBottom => FieldTop + FieldHeight;

    private double MaximumScrollY => ContentHeight - ViewportHeight;

    private bool CanScroll => IsAttached && ViewportHeight > 0 && MaximumScrollY > Tolerance;

    private bool IsFieldVisible => FieldTop >= ScrollY - Tolerance && FieldBottom <= ScrollY + ViewportHeight + Tolerance;

    public double? TargetScrollY()
    {
        if (!CanScroll || IsFieldVisible)
        {
            return null;
        }

        var target = Math.Clamp(FieldTop > ScrollY ? FieldBottom - ViewportHeight : FieldTop, 0, MaximumScrollY);
        return Math.Abs(target - ScrollY) > Tolerance ? target : null;
    }
}
