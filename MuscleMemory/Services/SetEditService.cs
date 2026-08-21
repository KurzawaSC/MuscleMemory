using System.Globalization;
using MuscleMemory.Constants;

namespace MuscleMemory.Services;

public sealed class SetEditService : ISetEditService
{
    public async Task<SetValues?> PromptForSetAsync(string title, double initialWeight, int initialReps)
    {
        string weightInput = await Shell.Current.DisplayPromptAsync(
            title, UiText.PromptEnterWeightKg, initialValue: initialWeight.ToString(), keyboard: Keyboard.Numeric);

        if (weightInput is null)
        {
            return null;
        }

        string repsInput = await Shell.Current.DisplayPromptAsync(
            title, UiText.PromptEnterReps, initialValue: initialReps.ToString(), keyboard: Keyboard.Numeric);

        if (repsInput is null)
        {
            return null;
        }

        if (!double.TryParse(weightInput, NumberStyles.Any, CultureInfo.InvariantCulture, out double weight)
            || !int.TryParse(repsInput, out int reps))
        {
            return null;
        }

        return new SetValues(weight, reps);
    }

    public Task<bool> ConfirmDeleteAsync() =>
        Shell.Current.DisplayAlertAsync(UiText.TitleDeleteSet, UiText.BodyDeleteSetConfirmation, UiText.ButtonYes, UiText.ButtonNo);
}
