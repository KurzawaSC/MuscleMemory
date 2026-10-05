using MuscleMemory.Constants;
using MuscleMemory.Controls;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Services;

public sealed class DialogService : IDialogService
{
    private const string NoHostMessage = "Neither the window nor the current page can host the dialog.";

    public Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText) =>
        ShowAsync(new ConfirmDialogViewModel(title, message, confirmText, cancelText));

    public Task ShowMessageAsync(string title, string message) =>
        ShowAsync(new ConfirmDialogViewModel(title, message, UiText.ButtonOk, null));

    private static async Task<bool> ShowAsync(ConfirmDialogViewModel viewModel)
    {
        var dialog = new ConfirmDialog { BindingContext = viewModel };
        var hide = Present(dialog);

        using (WindowLayer.InterceptBack(() => viewModel.CancelCommand.Execute(null)))
        {
            try
            {
                await dialog.AnimateInAsync();
                var result = await viewModel.Result;
                await dialog.AnimateOutAsync();
                return result;
            }
            finally
            {
                hide();
            }
        }
    }

    private static Action Present(ConfirmDialog dialog)
    {
        dialog.Parent = Shell.Current;
        if (WindowLayer.TryShow(dialog))
        {
            return () => WindowLayer.Hide(dialog);
        }

        dialog.Parent = null;
        return PresentOnPage(dialog);
    }

    private static Action PresentOnPage(ConfirmDialog dialog)
    {
        if (Shell.Current?.CurrentPage is not ContentPage { Content: Grid root })
        {
            throw new InvalidOperationException(NoHostMessage);
        }

        Grid.SetRowSpan(dialog, Math.Max(1, root.RowDefinitions.Count));
        Grid.SetColumnSpan(dialog, Math.Max(1, root.ColumnDefinitions.Count));
        root.Add(dialog);
        return () => root.Remove(dialog);
    }
}
