namespace MuscleMemory.Services;

public interface IUnsavedChangesGuard
{
    Task<bool> ConfirmDiscardAsync();
}
