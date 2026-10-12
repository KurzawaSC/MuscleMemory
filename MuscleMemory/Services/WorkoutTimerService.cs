using System.Globalization;
using MuscleMemory.Constants;

namespace MuscleMemory.Services;

public sealed class WorkoutTimerService(TimeProvider clock) : IWorkoutTimerService
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
        timer.Interval = UiTiming.TimerTick;
        timer.Tick += (_, _) => Ticked?.Invoke(this, EventArgs.Empty);
        return timer;
    }

    public string ElapsedSince(DateTime startTimeUtc) => FormatDuration(UtcNow - startTimeUtc);

    public TimeSpan RemainingUntil(DateTime endTimeUtc) => endTimeUtc - UtcNow;

    private DateTime UtcNow => clock.GetUtcNow().UtcDateTime;

    public string FormatDuration(TimeSpan duration)
    {
        var displayed = duration > TimeSpan.Zero ? duration : TimeSpan.Zero;
        return displayed.TotalHours >= 1
            ? string.Format(CultureInfo.CurrentCulture, DisplayFormats.ElapsedWithHours, displayed.Ticks / TimeSpan.TicksPerHour, displayed)
            : displayed.ToString(DisplayFormats.Elapsed);
    }
}
