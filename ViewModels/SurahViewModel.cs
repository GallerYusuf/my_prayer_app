using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrayerApp.Core.Contracts;
using PrayerApp.Core.Common;

namespace PrayerApp.ViewModels;

public partial class VerseItem : ObservableObject
{
    public required Verse Verse { get; init; }

    public int VerseNumber => Verse.VerseNumber;
    public string Text => Verse.Text;
    public string ArabicText => Verse.ArabicText;
    public bool HasArabic => Verse.ArabicText.Length > 0;
    public string PageInfo => Verse.Page > 0 ? $"Sayfa {Verse.Page} · Cüz {Verse.Juz}" : string.Empty;

    [ObservableProperty] private bool _isFavorite;
}

/// <summary>Verses of a single surah with Arabic text, favorites, sharing and reading-position memory.</summary>
[QueryProperty(nameof(SurahNumber), "surah")]
[QueryProperty(nameof(ScrollToVerse), "verse")]
public partial class SurahViewModel : ObservableObject
{
    private const string LastReadKey = "quran_last_read";
    private const string ShowArabicKey = "quran_show_arabic";
    private const string FontScaleKey = "quran_font_scale";

    private readonly IQuranRepository _quranRepository;
    private readonly IKeyValueStore _keyValueStore;
    private readonly IAudioPlayer _audioPlayer;
    private readonly IKhatmRepository _khatmRepository;
    private readonly IClock _clock;

    [ObservableProperty] private int _surahNumber;
    [ObservableProperty] private int _scrollToVerse;
    [ObservableProperty] private string _surahTitle = string.Empty;
    [ObservableProperty] private bool _isBusy;

    // Audio properties
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private bool _isAudioLoading;

    // Hatim properties
    [ObservableProperty] private bool _hasActiveKhatm;
    [ObservableProperty] private int _activeKhatmLastReadPage;
    [ObservableProperty] private int _currentVisiblePage;
    [ObservableProperty] private bool _showHatimUpdateChip;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ArabicFontSize))]
    [NotifyPropertyChangedFor(nameof(TranslationFontSize))]
    private double _fontScale = 1.0;

    [ObservableProperty] private bool _showArabic = true;

    public double ArabicFontSize => 24 * FontScale;
    public double TranslationFontSize => 15 * FontScale;

    public ObservableCollection<VerseItem> Verses { get; } = [];

    public SurahViewModel(
        IQuranRepository quranRepository,
        IKeyValueStore keyValueStore,
        IAudioPlayer audioPlayer,
        IKhatmRepository khatmRepository,
        IClock clock)
    {
        _quranRepository = quranRepository;
        _keyValueStore = keyValueStore;
        _audioPlayer = audioPlayer;
        _khatmRepository = khatmRepository;
        _clock = clock;

        ShowArabic = _keyValueStore.GetString(ShowArabicKey) != "0";
        if (double.TryParse(_keyValueStore.GetString(FontScaleKey), out var scale))
            FontScale = Math.Clamp(scale, 0.8, 1.6);
    }

    public async Task OnAppearingAsync()
    {
        await CheckHatimStatusAsync();

        if (Verses.Count > 0)
            return;

        try
        {
            IsBusy = true;
            var verses = await _quranRepository.GetVersesAsync(SurahNumber);
            if (verses.Count == 0)
                return;

            SurahTitle = $"{verses[0].SurahName} Suresi";

            foreach (var verse in verses)
            {
                Verses.Add(new VerseItem
                {
                    Verse = verse,
                    IsFavorite = await _quranRepository.IsFavoriteAsync(verse.SurahNumber, verse.VerseNumber)
                });
            }

            SaveLastRead(Math.Max(1, ScrollToVerse));
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Called by the page as the user scrolls; remembers the reading position.</summary>
    public void OnVisibleVerseChanged(int lastVisibleIndex)
    {
        if (lastVisibleIndex >= 0 && lastVisibleIndex < Verses.Count)
        {
            var verse = Verses[lastVisibleIndex].Verse;
            SaveLastRead(verse.VerseNumber);
            if (verse.Page != CurrentVisiblePage)
            {
                CurrentVisiblePage = verse.Page;
                UpdateHatimChipVisibility();
            }
        }
    }

    private void SaveLastRead(int verseNumber)
    {
        if (SurahNumber > 0)
            _keyValueStore.SetString(LastReadKey, $"{SurahNumber}:{verseNumber}");
    }

    [RelayCommand]
    private void ToggleArabic()
    {
        ShowArabic = !ShowArabic;
        _keyValueStore.SetString(ShowArabicKey, ShowArabic ? "1" : "0");
    }

    [RelayCommand]
    private void IncreaseFont() => SetFontScale(FontScale + 0.1);

    [RelayCommand]
    private void DecreaseFont() => SetFontScale(FontScale - 0.1);

    private void SetFontScale(double value)
    {
        FontScale = Math.Clamp(Math.Round(value, 1), 0.8, 1.6);
        _keyValueStore.SetString(FontScaleKey, FontScale.ToString("0.0"));
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(VerseItem item)
    {
        await _quranRepository.ToggleFavoriteAsync(item.Verse.SurahNumber, item.Verse.VerseNumber);
        item.IsFavorite = !item.IsFavorite;
    }

    [RelayCommand]
    private static async Task ShareVerseAsync(VerseItem item)
    {
        var arabic = item.HasArabic ? $"{item.ArabicText}\n\n" : string.Empty;
        var text = $"{arabic}\"{item.Text}\"\n\n— {item.Verse.Reference}\n\nEzan Vakti uygulaması";
        await Share.Default.RequestAsync(new ShareTextRequest { Title = "Ayet Paylaş", Text = text });
    }

    /// <summary>Last reading position as (surah, verse), or null when never read.</summary>
    public static (int Surah, int Verse)? GetLastRead(IKeyValueStore store)
    {
        var raw = store.GetString(LastReadKey);
        if (string.IsNullOrEmpty(raw))
            return null;

        var parts = raw.Split(':');
        if (parts.Length == 2 && int.TryParse(parts[0], out var surah) && int.TryParse(parts[1], out var verse))
            return (surah, verse);

        // Legacy format stored only the surah number.
        return int.TryParse(raw, out var legacySurah) ? (legacySurah, 1) : null;
    }

    // ---- Audio Player ----

    [RelayCommand]
    private async Task PlayAudioAsync()
    {
        if (IsAudioLoading) return;

        try
        {
            IsAudioLoading = true;

            // Register events
            _audioPlayer.PlaybackEnded += OnPlaybackEnded;
            _audioPlayer.PlaybackFailed += OnPlaybackFailed;

            var url = $"https://download.quranicaudio.com/quran/mishaari_raashid_al_3afasy/{SurahNumber:D3}.mp3";
            await _audioPlayer.PlayAsync(url);
            IsPlaying = true;
        }
        catch (Exception ex)
        {
            IsPlaying = false;
            await Shell.Current.DisplayAlertAsync("Hata", "Ses dosyası yüklenemedi. İnternet bağlantınızı kontrol edin.", "Tamam");
        }
        finally
        {
            IsAudioLoading = false;
        }
    }

    [RelayCommand]
    private async Task PauseAudioAsync()
    {
        await _audioPlayer.PauseAsync();
        IsPlaying = false;
    }

    [RelayCommand]
    private async Task StopAudioAsync()
    {
        await _audioPlayer.StopAsync();
        IsPlaying = false;
    }

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsPlaying = false;
        });
    }

    private void OnPlaybackFailed(object? sender, string error)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            IsPlaying = false;
            await Shell.Current.DisplayAlertAsync("Ses Hatası", "Ses oynatılırken bir hata oluştu.", "Tamam");
        });
    }

    public void OnDisappearing()
    {
        _audioPlayer.PlaybackEnded -= OnPlaybackEnded;
        _audioPlayer.PlaybackFailed -= OnPlaybackFailed;
        _ = _audioPlayer.StopAsync();
        IsPlaying = false;
    }

    // ---- Hatim Integration ----

    private async Task CheckHatimStatusAsync()
    {
        try
        {
            var plan = await _khatmRepository.GetActivePlanAsync();
            if (plan != null)
            {
                HasActiveKhatm = true;
                ActiveKhatmLastReadPage = plan.LastReadPage;
                UpdateHatimChipVisibility();
            }
            else
            {
                HasActiveKhatm = false;
                ShowHatimUpdateChip = false;
            }
        }
        catch
        {
            HasActiveKhatm = false;
            ShowHatimUpdateChip = false;
        }
    }

    private void UpdateHatimChipVisibility()
    {
        ShowHatimUpdateChip = HasActiveKhatm && CurrentVisiblePage > ActiveKhatmLastReadPage;
    }

    [RelayCommand]
    private async Task UpdateHatimPageAsync()
    {
        if (!HasActiveKhatm || CurrentVisiblePage <= ActiveKhatmLastReadPage)
            return;

        try
        {
            var plan = await _khatmRepository.GetActivePlanAsync();
            if (plan != null)
            {
                await _khatmRepository.SetLastReadPageAsync(plan.Id, CurrentVisiblePage, _clock.Today);
                ActiveKhatmLastReadPage = CurrentVisiblePage;
                UpdateHatimChipVisibility();
                
                await Shell.Current.DisplayAlertAsync("Hatim Güncellendi", $"Hatminiz sayfa {CurrentVisiblePage} olarak güncellendi.", "Tamam");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Hata", "Hatim ilerlemesi kaydedilemedi.", "Tamam");
        }
    }
}
