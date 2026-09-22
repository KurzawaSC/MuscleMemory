namespace MuscleMemory.Services;

public interface IDialogService
{
    Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText);

    Task ShowMessageAsync(string title, string message);
}
