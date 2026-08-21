namespace MuscleMemory.Services;

public sealed class NavigationStackService : INavigationStackService
{
    public void RemoveFromAllTabs<TPage>() where TPage : Page
    {
        var shell = Shell.Current;
        if (shell is null)
        {
            return;
        }

        foreach (var section in shell.Items.SelectMany(item => item.Items))
        {
            foreach (var page in section.Navigation.NavigationStack.OfType<TPage>().ToList())
            {
                section.Navigation.RemovePage(page);
            }
        }
    }
}
