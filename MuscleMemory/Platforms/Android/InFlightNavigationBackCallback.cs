using AndroidX.Activity;
using MuscleMemory.Services;

namespace MuscleMemory;

internal sealed class InFlightNavigationBackCallback : OnBackPressedCallback
{
    private readonly INavigationService _navigation;

    public InFlightNavigationBackCallback(INavigationService navigation) : base(navigation.IsNavigating)
    {
        _navigation = navigation;
        _navigation.IsNavigatingChanged += OnIsNavigatingChanged;
    }

    public override void HandleOnBackPressed()
    {
    }

    public void Release()
    {
        _navigation.IsNavigatingChanged -= OnIsNavigatingChanged;
        _navigation.AbandonInFlightNavigations();
        Remove();
    }

    private void OnIsNavigatingChanged(object? sender, EventArgs e) => Enabled = _navigation.IsNavigating;
}
