using PrayerApp.Core.Contracts;

namespace PrayerApp.Services;

public sealed class NullAudioPlayer : IAudioPlayer
{
    public bool IsPlaying => false;
    public string? CurrentUrl => null;

    #pragma warning disable CS0067 // Event is never used
    public event EventHandler? PlaybackEnded;
    public event EventHandler<string>? PlaybackFailed;
    #pragma warning restore CS0067

    public Task PlayAsync(string url)
    {
        return Task.CompletedTask;
    }

    public Task PauseAsync()
    {
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        return Task.CompletedTask;
    }
}
