using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Extensions;
using MuscleMemory.Models;
using MuscleMemory.Services;

namespace MuscleMemory.ViewModels;

public sealed partial class SetActionsViewModel : ObservableObject
{
    private readonly ISetActionsHost _host;
    private readonly IDialogService _dialogs;
    private readonly IErrorHandler _errors;
    private PendingSave? _pendingSave;

    public SetActionsViewModel(ISetActionsHost host, IDialogService dialogs, IErrorHandler errors)
    {
        _host = host;
        _dialogs = dialogs;
        _errors = errors;
        SaveEditorCommand.NotifyCanExecuteChangedWhen(Editor, nameof(SetInputViewModel.IsValid));
    }

    [ObservableProperty]
    public partial bool IsActionSheetOpen { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActionSetTitle))]
    [NotifyPropertyChangedFor(nameof(ActionSetSubtitle))]
    public partial WorkoutSet? ActionSet { get; private set; }

    public string ActionSetTitle => ActionSet is { } set ? string.Format(CultureInfo.CurrentCulture, UiText.SetProgressFormat, set.SetNumber) : string.Empty;

    public string ActionSetSubtitle => ActionSet is { } set ? string.Format(CultureInfo.CurrentCulture, UiText.LoggedSetFormat, set.Weight, set.Reps) : string.Empty;

    [ObservableProperty]
    public partial bool IsEditorOpen { get; set; }

    [ObservableProperty]
    public partial string EditorTitle { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string EditorConfirmText { get; private set; } = string.Empty;

    public SetInputViewModel Editor { get; } = new();

    public bool IsAnySheetOpen => IsActionSheetOpen || IsEditorOpen;

    private bool CanShow => _host.CanShowSetActions;

    private bool CanSaveEditor => Editor.IsValid;

    partial void OnIsEditorOpenChanged(bool value)
    {
        if (!value)
        {
            _pendingSave = null;
        }
    }

    public void OpenEditor(string title, string confirmText, WorkoutSet? prefill, int sessionExerciseId, Func<SetValues, Task> persistAsync)
    {
        _pendingSave = new PendingSave(sessionExerciseId, persistAsync);

        if (prefill is null)
        {
            Editor.Clear();
        }
        else
        {
            Editor.Fill(prefill.Weight, prefill.Reps);
        }

        EditorTitle = title;
        EditorConfirmText = confirmText;
        IsEditorOpen = true;
    }

    public void CloseSheets()
    {
        Cancel();
        CloseEditor();
    }

    [RelayCommand(CanExecute = nameof(CanShow))]
    private void Show(WorkoutSet set)
    {
        ActionSet = set;
        IsActionSheetOpen = true;
    }

    [RelayCommand]
    private void Cancel()
    {
        IsActionSheetOpen = false;
        ActionSet = null;
    }

    [RelayCommand]
    private Task EditAsync() => _errors.RunAsync(async () =>
    {
        if (await DismissAsync() is not { } set)
        {
            return;
        }

        OpenEditor(UiText.TitleEditSet, UiText.ButtonSave, set, set.SessionExerciseId, values => _host.UpdateSetAsync(set, values));
    });

    [RelayCommand]
    private Task DeleteAsync() => _errors.RunAsync(async () =>
    {
        if (await DismissAsync() is not { } set)
        {
            return;
        }

        if (!await _dialogs.ConfirmAsync(UiText.TitleDeleteSet, UiText.BodyDeleteSetConfirmation, UiText.ButtonDelete, UiText.ButtonCancel))
        {
            return;
        }

        await _host.DeleteSetAsync(set);
    });

    private async Task<WorkoutSet?> DismissAsync()
    {
        var set = ActionSet;
        await SheetTransition.CloseAsync(Cancel);
        return set;
    }

    [RelayCommand(CanExecute = nameof(CanSaveEditor))]
    private Task SaveEditorAsync() => _errors.RunAsync(async () =>
    {
        if (_pendingSave is not { } pending || !Editor.TryRead(out var values))
        {
            return;
        }

        await pending.PersistAsync(values);
        CloseEditor();
        await _host.RefreshSetsAsync(pending.SessionExerciseId);
    });

    [RelayCommand]
    private void CloseEditor()
    {
        IsEditorOpen = false;
    }

    private sealed record PendingSave(int SessionExerciseId, Func<SetValues, Task> PersistAsync);
}
