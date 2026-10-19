namespace MuscleMemory.Progression;

public sealed record SessionVolume
{
    public int SessionId { get; init; }
    public DateTime DateUtc { get; init; }
    public double Volume { get; init; }
}
