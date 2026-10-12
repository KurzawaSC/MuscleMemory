using MuscleMemory.Constants;
using MuscleMemory.Models;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Tests.ViewModels;

public sealed class SetActionsViewModelTests : IAsyncLifetime
{
    private readonly TestViewModels _ui = new();
    private WorkoutSet _logged = null!;

    private SetActionsViewModel Actions => _ui.ActiveWorkout.SetActions;

    public async ValueTask InitializeAsync()
    {
        var bench = await _ui.App.Data.ExerciseAsync("Bench Press");
        var push = await _ui.App.Data.WorkoutAsync("Push Day", bench);
        _ui.ActiveWorkout.ApplyQueryAttributes(new Dictionary<string, object> { [QueryKeys.Workout] = push });
        await _ui.SettleAsync();
        _ui.ActiveWorkout.SetInput.Fill(82.5, 5);
        await _ui.ActiveWorkout.SaveSetCommand.ExecuteAsync(null);
        _logged = _ui.ActiveWorkout.CurrentSets[0].Set;
    }

    public ValueTask DisposeAsync() => _ui.DisposeAsync();

    [Fact]
    public void Show_Set_OpensTheSheetWithItsTitle()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        var changed = PropertyChanges.Record(Actions);

        Actions.ShowCommand.Execute(_logged);

        Assert.True(Actions.IsActionSheetOpen);
        Assert.True(Actions.IsAnySheetOpen);
        Assert.Equal(("Set 1", "82.5 kg × 5"), (Actions.ActionSetTitle, Actions.ActionSetSubtitle));
        Assert.Contains(nameof(SetActionsViewModel.ActionSetTitle), changed);
    }

    [Fact]
    public void Cancel_SheetOpen_ClosesAndForgetsTheSet()
    {
        Actions.ShowCommand.Execute(_logged);

        Actions.CancelCommand.Execute(null);

        Assert.False(Actions.IsAnySheetOpen);
        Assert.Null(Actions.ActionSet);
        Assert.Equal((string.Empty, string.Empty), (Actions.ActionSetTitle, Actions.ActionSetSubtitle));
    }

    [Fact]
    public async Task SaveEditorCommand_EditorInput_CanExecuteOnlyWhenValid()
    {
        Actions.ShowCommand.Execute(_logged);
        await Actions.EditCommand.ExecuteAsync(null);
        var raised = 0;
        Actions.SaveEditorCommand.CanExecuteChanged += (_, _) => raised++;

        Actions.Editor.WeightInput = "501";
        Assert.False(Actions.SaveEditorCommand.CanExecute(null));

        Actions.Editor.WeightInput = "85";
        Assert.True(Actions.SaveEditorCommand.CanExecute(null));
        Assert.True(raised > 0);
    }

    [Fact]
    public async Task CloseEditor_BeforeSaving_DropsThePendingEdit()
    {
        Actions.ShowCommand.Execute(_logged);
        await Actions.EditCommand.ExecuteAsync(null);
        Actions.Editor.WeightInput = "90";

        Actions.CloseEditorCommand.Execute(null);
        await Actions.SaveEditorCommand.ExecuteAsync(null);

        Assert.False(Actions.IsEditorOpen);
        Assert.Equal(82.5, Assert.Single(await _ui.App.Sets.GetForSessionExerciseAsync(_logged.SessionExerciseId)).Weight);
    }

    [Fact]
    public async Task Delete_SheetAlreadyDismissed_DoesNothing()
    {
        _ui.AnswerConfirmations(true);

        await Actions.DeleteCommand.ExecuteAsync(null);

        Assert.Single(await _ui.App.Sets.GetForSessionExerciseAsync(_logged.SessionExerciseId));
    }

    [Fact]
    public void CloseSheets_BothOpen_ClosesBoth()
    {
        Actions.ShowCommand.Execute(_logged);
        Actions.OpenEditor("Title", "Save", _logged, _logged.SessionExerciseId, _ => Task.CompletedTask);

        Actions.CloseSheets();

        Assert.False(Actions.IsAnySheetOpen);
    }

    [Fact]
    public void OpenEditor_NoPrefill_StartsEmpty()
    {
        Actions.OpenEditor("Title", "Add", null, _logged.SessionExerciseId, _ => Task.CompletedTask);

        Assert.Equal((string.Empty, string.Empty), (Actions.Editor.WeightInput, Actions.Editor.RepsInput));
        Assert.Equal(("Title", "Add", true), (Actions.EditorTitle, Actions.EditorConfirmText, Actions.IsEditorOpen));
    }

    [Fact]
    public void RefreshText_Polish_ReformatsTheSheetTexts()
    {
        using (new CultureScope(Cultures.EnglishUs))
        {
            Actions.ShowCommand.Execute(_logged);
            Actions.OpenEditor("Title", "Save", _logged, _logged.SessionExerciseId, _ => Task.CompletedTask);
        }

        using var polish = new CultureScope(Cultures.Polish);
        var changed = PropertyChanges.Record(Actions);

        Actions.RefreshText();

        Assert.Equal(("Seria 1", "82,5 kg × 5"), (Actions.ActionSetTitle, Actions.ActionSetSubtitle));
        Assert.Equal("82,5", Actions.Editor.WeightInput);
        Assert.Contains(nameof(SetActionsViewModel.ActionSetSubtitle), changed);
    }
}
