using System.Windows.Input;
using PlatformView = Android.Views.View;

namespace MuscleMemory.Controls;

public class LongPressBehavior : PlatformBehavior<View, PlatformView>
{
    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(LongPressBehavior));

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(LongPressBehavior));

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    protected override void OnAttachedTo(View bindable, PlatformView platformView)
    {
        base.OnAttachedTo(bindable, platformView);
        platformView.HapticFeedbackEnabled = false;
        platformView.LongClick += OnLongClick;
    }

    protected override void OnDetachedFrom(View bindable, PlatformView platformView)
    {
        platformView.LongClick -= OnLongClick;
        platformView.HapticFeedbackEnabled = true;
        base.OnDetachedFrom(bindable, platformView);
    }

    private void OnLongClick(object? sender, PlatformView.LongClickEventArgs e)
    {
        e.Handled = true;

        if (Command?.CanExecute(CommandParameter) == true)
        {
            Command.Execute(CommandParameter);
        }
    }
}
