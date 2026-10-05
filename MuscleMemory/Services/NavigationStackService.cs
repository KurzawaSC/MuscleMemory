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

    public async Task<bool> ConfirmDiscardingChangesOnTabAsync(string tabRoute)
    {
        foreach (var guard in GuardsOnTab(tabRoute))
        {
            if (!await guard.ConfirmDiscardAsync())
            {
                return false;
            }
        }

        return true;
    }

    private static List<IUnsavedChangesGuard> GuardsOnTab(string tabRoute) =>
    [
        .. AllTabs()
            .Where(section => section.Items.Any(content => content.Route == tabRoute))
            .SelectMany(section => section.Navigation.NavigationStack)
            .Select(page => page?.BindingContext)
            .OfType<IUnsavedChangesGuard>()
    ];

    private static IEnumerable<ShellSection> AllTabs() =>
        Shell.Current?.Items.SelectMany(item => item.Items) ?? [];
}
