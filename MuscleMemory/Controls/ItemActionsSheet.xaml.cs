using System.Windows.Input;

namespace MuscleMemory.Controls;

public partial class ItemActionsSheet : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(ItemActionsSheet), string.Empty);

    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(ItemActionsSheet), string.Empty);

    public static readonly BindableProperty EditTextProperty =
        BindableProperty.Create(nameof(EditText), typeof(string), typeof(ItemActionsSheet), string.Empty);

    public static readonly BindableProperty DeleteTextProperty =
        BindableProperty.Create(nameof(DeleteText), typeof(string), typeof(ItemActionsSheet), string.Empty);

    public static readonly BindableProperty EditCommandProperty =
        BindableProperty.Create(nameof(EditCommand), typeof(ICommand), typeof(ItemActionsSheet));

    public static readonly BindableProperty HistoryCommandProperty =
        BindableProperty.Create(nameof(HistoryCommand), typeof(ICommand), typeof(ItemActionsSheet));

    public static readonly BindableProperty DeleteCommandProperty =
        BindableProperty.Create(nameof(DeleteCommand), typeof(ICommand), typeof(ItemActionsSheet));

    public static readonly BindableProperty CancelCommandProperty =
        BindableProperty.Create(nameof(CancelCommand), typeof(ICommand), typeof(ItemActionsSheet));

    public ItemActionsSheet() => InitializeComponent();

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string EditText
    {
        get => (string)GetValue(EditTextProperty);
        set => SetValue(EditTextProperty, value);
    }

    public string DeleteText
    {
        get => (string)GetValue(DeleteTextProperty);
        set => SetValue(DeleteTextProperty, value);
    }

    public ICommand? EditCommand
    {
        get => (ICommand?)GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }

    public ICommand? HistoryCommand
    {
        get => (ICommand?)GetValue(HistoryCommandProperty);
        set => SetValue(HistoryCommandProperty, value);
    }

    public ICommand? DeleteCommand
    {
        get => (ICommand?)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    public ICommand? CancelCommand
    {
        get => (ICommand?)GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }
}
