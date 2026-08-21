namespace MuscleMemory.Services;

public interface ISetEditService
{
    Task<SetValues?> PromptForSetAsync(string title, double initialWeight, int initialReps);
    Task<bool> ConfirmDeleteAsync();
}
