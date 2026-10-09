namespace MuscleMemory.Services;

public interface IDatabaseMaintenanceService
{
    Task<string?> CreateExportSnapshotAsync();
    Task<ImportPreview> PrepareImportAsync(Stream source);
    Task ImportAsync();
    Task DeleteImportCopiesAsync(string pickedFilePath);
    Task DeleteTemporaryFilesAsync();
    Task ClearAllDataAsync();
}
