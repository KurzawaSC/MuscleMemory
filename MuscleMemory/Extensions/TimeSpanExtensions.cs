namespace MuscleMemory.Extensions;

public static class TimeSpanExtensions
{
    public static uint ToAnimationLength(this TimeSpan duration) => (uint)duration.TotalMilliseconds;
}
