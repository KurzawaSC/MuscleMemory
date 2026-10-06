namespace MuscleMemory.Services;

public sealed class NavigationService : INavigationService
{
    private int _inFlight;
    private int _generation;

    public event EventHandler? IsNavigatingChanged;

    public bool IsNavigating => _inFlight > 0;

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
