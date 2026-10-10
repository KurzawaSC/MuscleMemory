namespace MuscleMemory.Services;

public sealed class NavigationStackService : INavigationStackService
{
    public void PopAllTabsToRoot()
    {
        foreach (var section in AllTabs())
        {
            foreach (var page in section.Navigation.NavigationStack.Skip(1).OfType<Page>().ToList())
            {
                section.Navigation.RemovePage(page);
            }
        }
    }

    public bool ContainsPageBoundTo(object bindingContext) =>
        AllTabs().Any(section => section.Navigation.NavigationStack.Any(page => page?.BindingContext == bindingContext));

    public Task<bool> ConfirmDiscardingChangesOnTabAsync(string tabRoute) =>
        ConfirmDiscardingAsync(GuardsOn(AllTabs().Where(section => section.Items.Any(content => content.Route == tabRoute))));

    public Task<bool> ConfirmDiscardingAllChangesAsync() => ConfirmDiscardingAsync(GuardsOn(AllTabs()));

    private static async Task<bool> ConfirmDiscardingAsync(List<IUnsavedChangesGuard> guards)
    {
        foreach (var guard in guards)
        {
            if (!await guard.ConfirmDiscardAsync())
            {
                return false;
            }
        }

        return true;
    }

    private static List<IUnsavedChangesGuard> GuardsOn(IEnumerable<ShellSection> tabs) =>
    [
        .. tabs
            .SelectMany(section => section.Navigation.NavigationStack)
            .Select(page => page?.BindingContext)
            .OfType<IUnsavedChangesGuard>()
    ];

    private static IEnumerable<ShellSection> AllTabs() =>
        Shell.Current?.Items.SelectMany(item => item.Items) ?? [];
}
