namespace MuscleMemory.Services;

public sealed class NavigationStackService : INavigationStackService
{
    public void RemoveFromAllTabs<TPage>() where TPage : Page
    {
        foreach (var section in AllTabs())
        {
            foreach (var page in section.Navigation.NavigationStack.OfType<TPage>().ToList())
            {
                section.Navigation.RemovePage(page);
            }
        }
    }

    public bool ContainsPageBoundTo(object bindingContext) =>
        AllTabs().Any(section => section.Navigation.NavigationStack.Any(page => page?.BindingContext == bindingContext));

    private static IEnumerable<ShellSection> AllTabs() =>
        Shell.Current?.Items.SelectMany(item => item.Items) ?? [];
}
