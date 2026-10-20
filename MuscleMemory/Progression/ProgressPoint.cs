namespace MuscleMemory.Progression;

public sealed record ProgressPoint(int SessionId, DateOnly Date, double Value)
{
    public int Day => Date.DayNumber;
}
