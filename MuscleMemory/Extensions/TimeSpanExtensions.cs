namespace MuscleMemory.Extensions;

public static class TimeSpanExtensions
{
    public static uint ToAnimationLength(this TimeSpan duration) => (uint)duration.TotalMilliseconds;

    public static TimeSpan RoundUpToWholeSecond(this TimeSpan duration) =>
        duration + TimeSpan.FromTicks((TimeSpan.TicksPerSecond - duration.Ticks % TimeSpan.TicksPerSecond) % TimeSpan.TicksPerSecond);
}
