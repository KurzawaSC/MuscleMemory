using System.Windows.Input;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Controls;

public partial class SetEditorSheet : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(SetEditorSheet), string.Empty);

    public static readonly BindableProperty ConfirmTextProperty =
        BindableProperty.Create(nameof(ConfirmText), typeof(string), typeof(SetEditorSheet), string.Empty);

    public static readonly BindableProperty EditorProperty =
        BindableProperty.Create(nameof(Editor), typeof(SetInputViewModel), typeof(SetEditorSheet));

    public static readonly BindableProperty ConfirmCommandProperty =
        BindableProperty.Create(nameof(ConfirmCommand), typeof(ICommand), typeof(SetEditorSheet));

    public static readonly BindableProperty CancelCommandProperty =
        BindableProperty.Create(nameof(CancelCommand), typeof(ICommand), typeof(SetEditorSheet));

    public SetEditorSheet() => InitializeComponent();

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string ConfirmText
    {
        get => (string)GetValue(ConfirmTextProperty);
        set => SetValue(ConfirmTextProperty, value);
    }

    public SetInputViewModel? Editor
    {
        get => (SetInputViewModel?)GetValue(EditorProperty);
        set => SetValue(EditorProperty, value);
    }

    public ICommand? ConfirmCommand
    {
        get => (ICommand?)GetValue(ConfirmCommandProperty);
        set => SetValue(ConfirmCommandProperty, value);
    }

    public ICommand? CancelCommand
    {
        get => (ICommand?)GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }
}
