using MuscleMemory.ViewModels;

namespace MuscleMemory.Tests.ViewModels;

public sealed class ConfirmDialogViewModelTests
{
    private const int TimeoutMilliseconds = 10_000;
    private const string Title = "Delete set";
    private const string Message = "Are you sure?";
    private const string ConfirmText = "Delete";
    private const string CancelText = "Cancel";

    [Fact]
    public void New_WithCancel_ShowsBothButtons()
    {
        var dialog = new ConfirmDialogViewModel(Title, Message, ConfirmText, CancelText);

        Assert.Equal((Title, Message, ConfirmText, CancelText, true), (dialog.Title, dialog.Message, dialog.ConfirmText, dialog.CancelText, dialog.HasCancel));
        Assert.False(dialog.Result.IsCompleted);
    }

    [Fact]
    public void New_MessageOnly_HasNoCancel()
    {
        var dialog = new ConfirmDialogViewModel(Title, Message, ConfirmText, null);

        Assert.Equal((string.Empty, false), (dialog.CancelText, dialog.HasCancel));
    }

    [Theory(Timeout = TimeoutMilliseconds)]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Answer_FirstOne_IsTheResultAndLaterOnesAreIgnored(bool confirm)
    {
        var dialog = new ConfirmDialogViewModel(Title, Message, ConfirmText, CancelText);

        (confirm ? dialog.ConfirmCommand : dialog.CancelCommand).Execute(null);
        (confirm ? dialog.CancelCommand : dialog.ConfirmCommand).Execute(null);

        Assert.Equal(confirm, await dialog.Result.WaitAsync(TestContext.Current.CancellationToken));
    }
}
