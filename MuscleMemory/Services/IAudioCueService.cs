namespace MuscleMemory.Services;

public interface IAudioCueService
{
    Task PlayBreakEndAsync();
    void Stop();
}
