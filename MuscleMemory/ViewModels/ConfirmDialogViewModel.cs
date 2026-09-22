using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MuscleMemory.ViewModels;

public sealed partial class ConfirmDialogViewModel(string title, string message, string confirmText, string? cancelText) : ObservableObject
{
    private readonly TaskCompletionSource<bool> _result = new();

    public string Title { get; } = title;

    public string Message { get; } = message;

    public string ConfirmText { get; } = confirmText;

    public string CancelText { get; } = cancelText ?? string.Empty;

    public bool HasCancel { get; } = cancelText is not null;

    public Task<bool> Result => _result.Task;

    [RelayCommand]
    private void Confirm() => _result.TrySetResult(true);

    [RelayCommand]
    private void Cancel() => _result.TrySetResult(false);
}
