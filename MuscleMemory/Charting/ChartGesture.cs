namespace MuscleMemory.Charting;

public sealed class ChartGesture(float touchSlop)
{
    private float _startX;
    private float _startY;

    public ChartGestureKind Kind { get; private set; } = ChartGestureKind.None;

    public void Begin(float x, float y)
    {
        _startX = x;
        _startY = y;
        Kind = ChartGestureKind.Pending;
    }

    public ChartGestureKind Move(float x, float y)
    {
        if (Kind == ChartGestureKind.Pending)
        {
            Kind = Classify(x - _startX, y - _startY);
        }

        return Kind;
    }

    public bool EndAsTap(float x, float y)
    {
        var isTap = Move(x, y) == ChartGestureKind.Pending;
        Kind = ChartGestureKind.None;
        return isTap;
    }

    public void Cancel() => Kind = ChartGestureKind.None;

    private ChartGestureKind Classify(float dx, float dy)
    {
        if ((dx * dx) + (dy * dy) <= touchSlop * touchSlop)
        {
            return ChartGestureKind.Pending;
        }

        return Math.Abs(dx) > Math.Abs(dy) ? ChartGestureKind.HorizontalDrag : ChartGestureKind.VerticalScroll;
    }
}
