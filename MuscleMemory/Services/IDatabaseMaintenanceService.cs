namespace MuscleMemory.Services;

public interface IDatabaseMaintenanceService
{
    Task<string?> CreateExportSnapshotAsync();
    Task ClearAllDataAsync();
}
