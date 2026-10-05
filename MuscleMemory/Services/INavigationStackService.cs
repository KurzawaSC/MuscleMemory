namespace MuscleMemory.Services;

public interface INavigationStackService
{
    void RemoveFromAllTabs<TPage>() where TPage : Page;

    bool ContainsPageBoundTo(object bindingContext);
}
