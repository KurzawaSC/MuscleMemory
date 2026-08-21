namespace MuscleMemory.Services;

public interface IWorkoutTimerService
{
    event EventHandler? Ticked;
    void Start();
    void Stop();
    string ElapsedSince(DateTime startTimeUtc);
    string FormatElapsed(TimeSpan elapsed);
    TimeSpan RemainingUntil(DateTime endTimeUtc);
    string FormatCountdown(TimeSpan remaining);
}
