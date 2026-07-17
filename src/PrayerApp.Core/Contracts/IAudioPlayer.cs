namespace PrayerApp.Core.Contracts;

public interface IAudioPlayer
{
    bool IsPlaying { get; }
    string? CurrentUrl { get; }
    Task PlayAsync(string url);
    Task PauseAsync();
    Task StopAsync();
    event EventHandler PlaybackEnded;
    event EventHandler<string> PlaybackFailed;
}
