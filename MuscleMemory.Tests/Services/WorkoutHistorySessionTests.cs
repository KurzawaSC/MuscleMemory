using MuscleMemory.Services;

namespace MuscleMemory.Tests.Services;

public sealed class WorkoutHistorySessionTests
{
    private static readonly DateTime StartUtc = new(2026, 3, 5, 13, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Duration_StartAndEnd_ReturnsDifference()
    {
        var session = new WorkoutHistorySession(1, StartUtc, StartUtc.AddMinutes(47), 0, []);

        Assert.Equal(TimeSpan.FromMinutes(47), session.Duration);
    }

    [Fact]
    public void LocalStartTime_UtcStart_ConvertsToLocalTime()
    {
        var session = new WorkoutHistorySession(1, StartUtc, StartUtc, 0, []);

        Assert.Equal(StartUtc.ToLocalTime(), session.LocalStartTime);
        Assert.Equal(DateTimeKind.Local, session.LocalStartTime.Kind);
    }
}
