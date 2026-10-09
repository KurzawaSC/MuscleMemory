namespace MuscleMemory.Services;

public sealed record ImportPreview(ImportFileStatus Status, ImportCounts Counts)
{
    public static ImportPreview Rejected(ImportFileStatus status) => new(status, ImportCounts.None);
}
