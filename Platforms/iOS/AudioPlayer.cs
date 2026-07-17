using AVFoundation;
using Foundation;
using PrayerApp.Core.Contracts;

namespace PrayerApp.Platforms.iOS;

public sealed class AudioPlayer : IAudioPlayer, IDisposable
{
    private AVPlayer? _player;
    private NSObject? _didPlayToEndObserver;
    private string? _currentUrl;
    private bool _isPlaying;

    public bool IsPlaying => _isPlaying;
    public string? CurrentUrl => _currentUrl;

    public event EventHandler? PlaybackEnded;
    public event EventHandler<string>? PlaybackFailed;

    public Task PlayAsync(string url)
    {
        try
        {
            if (_currentUrl == url && _player != null)
            {
                if (!_isPlaying)
                {
                    _player.Play();
                    _isPlaying = true;
                }
                return Task.CompletedTask;
            }

            StopAsync();

            _currentUrl = url;

            var nsUrl = NSUrl.FromString(url);
            if (nsUrl == null)
            {
                throw new ArgumentException("Invalid URL path", nameof(url));
            }

            var playerItem = AVPlayerItem.FromUrl(nsUrl);
            _player = AVPlayer.FromPlayerItem(playerItem);

            // Configure AVAudioSession for playback
            var session = AVAudioSession.SharedInstance();
            session.SetCategory(AVAudioSessionCategory.Playback, out _);
            session.SetActive(true, out _);

            _didPlayToEndObserver = NSNotificationCenter.DefaultCenter.AddObserver(
                AVPlayerItem.DidPlayToEndTimeNotification,
                OnPlaybackEnded,
                playerItem);

            _player.Play();
            _isPlaying = true;
        }
        catch (Exception ex)
        {
            PlaybackFailed?.Invoke(this, ex.Message);
            throw;
        }

        return Task.CompletedTask;
    }

    public Task PauseAsync()
    {
        if (_player != null && _isPlaying)
        {
            _player.Pause();
            _isPlaying = false;
        }
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (_player != null)
        {
            _player.Pause();
            _player.Seek(CoreMedia.CMTime.Zero);
            _isPlaying = false;
        }

        if (_didPlayToEndObserver != null)
        {
            NSNotificationCenter.DefaultCenter.RemoveObserver(_didPlayToEndObserver);
            _didPlayToEndObserver.Dispose();
            _didPlayToEndObserver = null;
        }

        _player?.Dispose();
        _player = null;
        _currentUrl = null;

        return Task.CompletedTask;
    }

    private void OnPlaybackEnded(NSNotification notification)
    {
        _isPlaying = false;
        PlaybackEnded?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _ = StopAsync();
    }
}
