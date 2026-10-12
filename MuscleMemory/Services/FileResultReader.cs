namespace MuscleMemory.Services;

public sealed class FileResultReader : IFileResultReader
{
    public Task<Stream> OpenReadAsync(FileResult file) => file.OpenReadAsync();
}
