using MuscleMemory.Constants;
using MuscleMemory.Controls;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Services;

public sealed class DialogService : IDialogService
{
    public Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText) =>
        ShowAsync(new ConfirmDialogViewModel(title, message, confirmText, cancelText));

    public Task ShowMessageAsync(string title, string message) =>
        ShowAsync(new ConfirmDialogViewModel(title, message, UiText.ButtonOk, null));

    private static async Task<bool> ShowAsync(ConfirmDialogViewModel viewModel)
    {
        var dialog = new ConfirmDialog
        {
            BindingContext = viewModel,
            Parent = Shell.Current
        };

        if (!WindowLayer.TryShow(dialog))
        {
            return false;
        }

        using (WindowLayer.InterceptBack(() => viewModel.CancelCommand.Execute(null)))
        {
            await dialog.AnimateInAsync();
            var result = await viewModel.Result;
            await dialog.AnimateOutAsync();
            WindowLayer.Hide(dialog);
            return result;
        }
    }
}
