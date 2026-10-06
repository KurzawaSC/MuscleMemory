namespace MuscleMemory.Services;

public sealed class NavigationService : INavigationService
{
    public Task GoToAsync(ShellNavigationState state) => Shell.Current.GoToAsync(state);

    public Task GoToAsync(ShellNavigationState state, IDictionary<string, object> parameters) =>
        Shell.Current.GoToAsync(state, parameters);
}
