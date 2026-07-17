using Android.Media;
using PrayerApp.Core.Contracts;

namespace PrayerApp.Platforms.Android;

public sealed class AudioPlayer : IAudioPlayer, IDisposable
{
    private MediaPlayer? _mediaPlayer;
    private string? _currentUrl;
    private bool _isPrepared;

    public bool IsPlaying => _mediaPlayer?.IsPlaying ?? false;
    public string? CurrentUrl => _currentUrl;

    public event EventHandler? PlaybackEnded;
    public event EventHandler<string>? PlaybackFailed;

    public async Task PlayAsync(string url)
    {
        try
        {
            if (_currentUrl == url && _mediaPlayer != null)
            {
                if (!_mediaPlayer.IsPlaying)
                {
                    _mediaPlayer.Start();
                }
                return;
            }

            await StopAsync();

            _currentUrl = url;
            _isPrepared = false;
            _mediaPlayer = new MediaPlayer();
            
            // Set audio attributes for media playback
            var audioAttributes = new AudioAttributes.Builder()
                .SetUsage(AudioUsageKind.Media)
                .SetContentType(AudioContentType.Music)
                .Build();
            _mediaPlayer.SetAudioAttributes(audioAttributes);

            _mediaPlayer.SetDataSource(url);

            var tcs = new TaskCompletionSource<bool>();
            
            _mediaPlayer.Prepared += (s, e) =>
            {
                _isPrepared = true;
                _mediaPlayer.Start();
                tcs.TrySetResult(true);
            };

            _mediaPlayer.Error += (s, e) =>
            {
                var errorMsg = $"MediaPlayer error: {e.What}, Extra: {e.Extra}";
                PlaybackFailed?.Invoke(this, errorMsg);
                tcs.TrySetException(new Exception(errorMsg));
                return true; // handled
            };

            _mediaPlayer.Completion += (s, e) =>
            {
                PlaybackEnded?.Invoke(this, EventArgs.Empty);
            };

            _mediaPlayer.PrepareAsync();

            await tcs.Task;
        }
        catch (Exception ex)
        {
            PlaybackFailed?.Invoke(this, ex.Message);
            throw;
        }
    }

    public Task PauseAsync()
    {
        if (_mediaPlayer != null && _mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Pause();
        }
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        try
        {
            if (_mediaPlayer != null)
            {
                if (_mediaPlayer.IsPlaying)
                {
                    _mediaPlayer.Stop();
                }
                _mediaPlayer.Release();
                _mediaPlayer.Dispose();
                _mediaPlayer = null;
            }
        }
        catch
        {
            // Ignore errors on release
        }
        _currentUrl = null;
        _isPrepared = false;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _ = StopAsync();
    }
}
