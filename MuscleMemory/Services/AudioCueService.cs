using Plugin.Maui.Audio;

namespace MuscleMemory.Services;

public sealed class AudioCueService(IAudioManager audioManager) : IAudioCueService
{
    private const string BreakEndFileName = "BreakEnd.mp3";

    private IAudioPlayer? _player;
    private Stream? _audioStream;
    private int _playbackRequest;

    public async Task PlayBreakEndAsync()
    {
        var request = ++_playbackRequest;
        var audioStream = await FileSystem.OpenAppPackageFileAsync(BreakEndFileName);

        if (request != _playbackRequest)
        {
            await audioStream.DisposeAsync();
            return;
        }

        Release();
        _audioStream = audioStream;
        Play(audioStream);
    }

    public void Stop()
    {
        _playbackRequest++;
        Release();
    }

    private void Play(Stream audioStream)
    {
        try
        {
            _player = audioManager.CreatePlayer(audioStream);
            _player.Play();
        }
        catch
        {
            Release();
            throw;
        }
    }

    private void Release()
    {
        _player?.Dispose();
        _player = null;
        _audioStream?.Dispose();
        _audioStream = null;
    }
}
