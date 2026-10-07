namespace MuscleMemory.Services;

public interface INavigationService
{
    event EventHandler? IsNavigatingChanged;

    event EventHandler<ShellNavigatingEventArgs>? Navigating;

    event EventHandler<ShellNavigatedEventArgs>? Navigated;

    bool IsNavigating { get; }

    ShellNavigationState CurrentState { get; }

    Task GoToAsync(ShellNavigationState state);

    Task GoToAsync(ShellNavigationState state, IDictionary<string, object> parameters);

    void AbandonInFlightNavigations();
}
