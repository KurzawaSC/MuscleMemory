namespace MuscleMemory.Services;

public sealed class NavigationService : INavigationService
{
    private int _inFlight;
    private int _generation;

    public event EventHandler? IsNavigatingChanged;

    public event EventHandler<ShellNavigatingEventArgs>? Navigating
    {
        add => Shell.Current.Navigating += value;
        remove => Shell.Current.Navigating -= value;
    }

    public event EventHandler<ShellNavigatedEventArgs>? Navigated
    {
        add => Shell.Current.Navigated += value;
        remove => Shell.Current.Navigated -= value;
    }

    public bool IsNavigating => _inFlight > 0;

    public ShellNavigationState CurrentState => Shell.Current.CurrentState;

    public Task GoToAsync(ShellNavigationState state) =>
        TrackAsync(() => Shell.Current.GoToAsync(state));

    public Task GoToAsync(ShellNavigationState state, IDictionary<string, object> parameters) =>
        TrackAsync(() => Shell.Current.GoToAsync(state, parameters));

    public void AbandonInFlightNavigations()
    {
        _generation++;
        SetInFlight(0);
    }

    private async Task TrackAsync(Func<Task> navigate)
    {
        var generation = _generation;
        SetInFlight(_inFlight + 1);

        try
        {
            await navigate();
        }
        finally
        {
            if (generation == _generation)
            {
                SetInFlight(_inFlight - 1);
            }
        }
    }

    private void SetInFlight(int inFlight)
    {
        var wasNavigating = IsNavigating;
        _inFlight = inFlight;

        if (wasNavigating != IsNavigating)
        {
            IsNavigatingChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
