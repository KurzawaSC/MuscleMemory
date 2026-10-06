namespace MuscleMemory.Services;

public interface INavigationService
{
    event EventHandler? IsNavigatingChanged;

    bool IsNavigating { get; }

    Task GoToAsync(ShellNavigationState state);

    Task GoToAsync(ShellNavigationState state, IDictionary<string, object> parameters);

    void AbandonInFlightNavigations();
}
