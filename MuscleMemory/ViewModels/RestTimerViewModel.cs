using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Diagnostics;
using MuscleMemory.Models;
using MuscleMemory.Services;

namespace MuscleMemory.ViewModels;

public sealed partial class RestTimerViewModel : ObservableObject
{
    private readonly IRestTimerHost _host;
    private readonly IWorkoutTimerService _timer;
    private readonly IHapticService _haptics;
    private readonly IAudioCueService _audioCues;
    private readonly IErrorHandler _errors;

    public RestTimerViewModel(IRestTimerHost host, IWorkoutTimerService timer, IHapticService haptics, IAudioCueService audioCues, IErrorHandler errors)
    {
        _host = host;
        _timer = timer;
        _haptics = haptics;
        _audioCues = audioCues;
        _errors = errors;
        TimerText = ZeroTimeText;
    }

    private string ZeroTimeText => _timer.FormatDuration(TimeSpan.Zero);

    public DateTime BreakEndTimeUtc { get; private set; }

    public int DurationSeconds { get; private set; }

    [ObservableProperty]
    public partial bool IsResting { get; private set; }

    [ObservableProperty]
    public partial string TimerText { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string TotalText { get; private set; } = string.Empty;

    public string ExtensionText { get; } = string.Format(CultureInfo.CurrentCulture, UiText.RestExtensionFormat, DomainDefaults.RestExtensionInSeconds);

    [ObservableProperty]
    public partial double Progress { get; private set; }

    [ObservableProperty]
    public partial bool IsEnding { get; private set; }

    public void Start(int durationSeconds) => Show(DateTime.UtcNow.AddSeconds(durationSeconds), durationSeconds);

    public void Restore(ActiveWorkoutState state)
    {
        if (!state.IsResting || _timer.RemainingUntil(state.BreakEndTimeUtc) <= TimeSpan.Zero)
        {
            Clear();
            return;
        }

        Show(state.BreakEndTimeUtc, state.RestDurationSeconds);
    }

    public void Tick()
    {
        if (!IsResting)
        {
            return;
        }

        if (_timer.RemainingUntil(BreakEndTimeUtc).TotalSeconds > 0)
        {
            UpdateCountdown();
            return;
        }

        Clear();
        _haptics.RestFinished();
        AppLog.LogFailures(_audioCues.PlayBreakEndAsync());
        _errors.ReportFailures(_host.SaveStateAsync());
    }

    public void Clear()
    {
        IsResting = false;
        BreakEndTimeUtc = default;
        DurationSeconds = 0;
        Progress = 0;
        IsEnding = false;
        TimerText = ZeroTimeText;
        TotalText = string.Empty;
    }

    [RelayCommand]
    private Task ExtendAsync() => _errors.RunAsync(async () =>
    {
        if (!IsResting)
        {
            return;
        }

        DurationSeconds += DomainDefaults.RestExtensionInSeconds;
        BreakEndTimeUtc = BreakEndTimeUtc.AddSeconds(DomainDefaults.RestExtensionInSeconds);
        TotalText = FormatTotal(DurationSeconds);
        UpdateCountdown();
        await _host.SaveStateAsync();
    });

    [RelayCommand]
    private Task SkipAsync() => _errors.RunAsync(async () =>
    {
        Clear();

        _audioCues.Stop();
        await _host.SaveStateAsync();
    });

    private void Show(DateTime breakEndTimeUtc, int durationSeconds)
    {
        DurationSeconds = durationSeconds;
        BreakEndTimeUtc = breakEndTimeUtc;
        TotalText = FormatTotal(durationSeconds);
        IsResting = true;
        UpdateCountdown();
    }

    private void UpdateCountdown()
    {
        var remaining = _timer.RemainingUntil(BreakEndTimeUtc);
        TimerText = _timer.FormatDuration(remaining);
        IsEnding = remaining <= UiTiming.RestEndingPulse;
        Progress = DurationSeconds > 0
            ? Math.Clamp(remaining.TotalSeconds / DurationSeconds, 0, 1)
            : 0;
    }

    private static string FormatTotal(int durationSeconds) =>
        string.Format(CultureInfo.CurrentCulture, UiText.RestTotalFormat, durationSeconds);
}
