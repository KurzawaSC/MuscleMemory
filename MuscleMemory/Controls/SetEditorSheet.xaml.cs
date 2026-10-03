using MuscleMemory.ViewModels;

namespace MuscleMemory.Controls;

public partial class SetEditorSheet : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(SetEditorSheet), string.Empty);

    public static readonly BindableProperty EditorProperty =
        BindableProperty.Create(nameof(Editor), typeof(SetInputViewModel), typeof(SetEditorSheet));

    public SetEditorSheet() => InitializeComponent();

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public SetInputViewModel? Editor
    {
        get => (SetInputViewModel?)GetValue(EditorProperty);
        set => SetValue(EditorProperty, value);
    }
}
