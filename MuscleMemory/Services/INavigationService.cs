namespace MuscleMemory.Services;

public interface INavigationService
{
    Task GoToAsync(ShellNavigationState state);

    Task GoToAsync(ShellNavigationState state, IDictionary<string, object> parameters);
}
