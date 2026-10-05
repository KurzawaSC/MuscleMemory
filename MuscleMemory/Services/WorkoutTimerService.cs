using MuscleMemory.Constants;

namespace MuscleMemory.Services;

public sealed class WorkoutTimerService : IWorkoutTimerService
{
    private const string NoDispatcherMessage = "The workout timer needs a running application to start.";

    private IDispatcherTimer? _timer;

    public event EventHandler? Ticked;

    public void Start() => (_timer ??= CreateTimer()).Start();

    public void Stop() => _timer?.Stop();

    private IDispatcherTimer CreateTimer()
    {
        var dispatcher = Application.Current?.Dispatcher ?? throw new InvalidOperationException(NoDispatcherMessage);
        var timer = dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Tick += (_, _) => Ticked?.Invoke(this, EventArgs.Empty);
        return timer;
    }

    public string ElapsedSince(DateTime startTimeUtc) => FormatElapsed(DateTime.UtcNow - startTimeUtc);

    public string FormatElapsed(TimeSpan elapsed) => Format(elapsed);

    public TimeSpan RemainingUntil(DateTime endTimeUtc) => endTimeUtc - DateTime.UtcNow;

    public string FormatCountdown(TimeSpan remaining) => Format(remaining);

    private static string Format(TimeSpan duration)
    {
        var displayed = duration > TimeSpan.Zero ? duration : TimeSpan.Zero;
        return displayed.ToString(displayed.TotalHours >= 1 ? UiText.ElapsedWithHoursFormat : UiText.ElapsedFormat);
    }
}
