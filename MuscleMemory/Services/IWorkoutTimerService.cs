namespace MuscleMemory.Services;

public interface IWorkoutTimerService
{
    event EventHandler? Ticked;
    void Start();
    void Stop();
    string ElapsedSince(DateTime startTimeUtc);
    TimeSpan RemainingUntil(DateTime endTimeUtc);
    string FormatDuration(TimeSpan duration);
}
