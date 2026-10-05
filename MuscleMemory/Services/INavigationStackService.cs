namespace MuscleMemory.Services;

public interface INavigationStackService
{
    void PopAllTabsToRoot();

    bool ContainsPageBoundTo(object bindingContext);

    Task<bool> ConfirmDiscardingChangesOnTabAsync(string tabRoute);
}
