using System.Diagnostics;
using Plugin.Maui.Audio;

namespace MuscleMemory.Services;

public sealed class AudioCueService(IAudioManager audioManager) : IAudioCueService
{
    private const string BreakEndFileName = "BreakEnd.mp3";

    private IAudioPlayer? _player;

    public async Task PlayBreakEndAsync()
    {
        try
        {
            var audioStream = await FileSystem.OpenAppPackageFileAsync(BreakEndFileName);
            Stop();
            _player = audioManager.CreatePlayer(audioStream);
            _player.Play();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to play break sound: {ex.Message}");
        }
    }

    public void Stop()
    {
        _player?.Dispose();
        _player = null;
    }
}
