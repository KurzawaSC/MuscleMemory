namespace MuscleMemory.Services;

public interface IFileResultReader
{
    Task<Stream> OpenReadAsync(FileResult file);
}
