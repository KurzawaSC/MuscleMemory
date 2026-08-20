namespace MuscleMemory.Services;

public interface IDatabaseMaintenanceService
{
    string DatabaseFilePath { get; }
    Task ClearAllDataAsync();
}
