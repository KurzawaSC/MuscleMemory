using MuscleMemory.Constants;

namespace MuscleMemory.Services;

public sealed class WorkoutTimerService : IWorkoutTimerService
{
    private readonly IDispatcherTimer _timer;

    public event EventHandler? Ticked;

    public WorkoutTimerService()
    {
        _timer = Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) => Ticked?.Invoke(this, EventArgs.Empty);
    }

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    public string ElapsedSince(DateTime startTimeUtc) => FormatElapsed(DateTime.UtcNow - startTimeUtc);

    public string FormatElapsed(TimeSpan elapsed) =>
        elapsed.ToString(elapsed.TotalHours >= 1 ? UiText.ElapsedWithHoursFormat : UiText.ElapsedFormat);

    public TimeSpan RemainingUntil(DateTime endTimeUtc) => endTimeUtc - DateTime.UtcNow;

    public string FormatCountdown(TimeSpan remaining) => remaining.ToString(UiText.ElapsedFormat);
}
