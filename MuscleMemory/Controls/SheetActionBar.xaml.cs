using System.Windows.Input;

namespace MuscleMemory.Controls;

public partial class SheetActionBar : ContentView
{
    public static readonly BindableProperty ConfirmTextProperty =
        BindableProperty.Create(nameof(ConfirmText), typeof(string), typeof(SheetActionBar), string.Empty);

    public static readonly BindableProperty CancelCommandProperty =
        BindableProperty.Create(nameof(CancelCommand), typeof(ICommand), typeof(SheetActionBar));

    public static readonly BindableProperty ConfirmCommandProperty =
        BindableProperty.Create(nameof(ConfirmCommand), typeof(ICommand), typeof(SheetActionBar));

    public static readonly BindableProperty IsConfirmEnabledProperty =
        BindableProperty.Create(nameof(IsConfirmEnabled), typeof(bool), typeof(SheetActionBar), true);

    public SheetActionBar() => InitializeComponent();

    public bool IsConfirmEnabled
    {
        get => (bool)GetValue(IsConfirmEnabledProperty);
        set => SetValue(IsConfirmEnabledProperty, value);
    }

    public string ConfirmText
    {
        get => (string)GetValue(ConfirmTextProperty);
        set => SetValue(ConfirmTextProperty, value);
    }

    public ICommand? CancelCommand
    {
        get => (ICommand?)GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    public ICommand? ConfirmCommand
    {
        get => (ICommand?)GetValue(ConfirmCommandProperty);
        set => SetValue(ConfirmCommandProperty, value);
    }
}
