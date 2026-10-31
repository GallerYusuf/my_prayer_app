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

/// <summary>Verses of a single surah with independent Arabic/Meal typography, focus mode, audio state clarity, and reading-position memory.</summary>
[QueryProperty(nameof(SurahNumber), "surah")]
[QueryProperty(nameof(ScrollToVerse), "verse")]
public partial class SurahViewModel : ObservableObject
{
    private const string LastReadKey = "quran_last_read";
    private const string ShowArabicKey = "quran_show_arabic";
    private const string ShowTranslationKey = "quran_show_translation";
    private const string FontScaleKey = "quran_font_scale";
    private const string ArabicFontSizeKey = "quran_arabic_font_size";
    private const string TranslationFontSizeKey = "quran_translation_font_size";

    private readonly IQuranRepository _quranRepository;
    private readonly IKeyValueStore _keyValueStore;
    private readonly IAudioPlayer _audioPlayer;
    private readonly IKhatmRepository _khatmRepository;
    private readonly IClock _clock;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoPreviousSurah))]
    [NotifyPropertyChangedFor(nameof(CanGoNextSurah))]
    private int _surahNumber;

    [ObservableProperty] private int _scrollToVerse;
    [ObservableProperty] private string _surahTitle = string.Empty;
    [ObservableProperty] private string _surahSubtitle = "Çevrimdışı metin · Ses için internet gerekir";
    [ObservableProperty] private bool _isBusy;

    public bool CanGoPreviousSurah => SurahNumber > 1;
    public bool CanGoNextSurah => SurahNumber is >= 1 and < 114;

    // Reading mode & appearance panel
    [ObservableProperty] private bool _isFocusMode;
    [ObservableProperty] private bool _isSettingsPanelOpen;
    [ObservableProperty] private bool _showArabic = true;
    [ObservableProperty] private bool _showTranslation = true;
    [ObservableProperty] private double _arabicFontSize = 26;
    [ObservableProperty] private double _translationFontSize = 16;

    [ObservableProperty] private double _fontScale = 1.0;

    // Audio properties
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAudioBarVisible))]
    private bool _isPlaying;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAudioBarVisible))]
    private bool _isAudioLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAudioBarVisible))]
    private bool _isAudioPaused;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAudioBarVisible))]
    private string _audioErrorMessage = string.Empty;

    [ObservableProperty] private string _audioStatusText = "Mishary Rashid Al-Afasy (İnternet gerekir)";

    public bool IsAudioBarVisible => IsAudioLoading || IsPlaying || IsAudioPaused || !string.IsNullOrEmpty(AudioErrorMessage);

    // Hatim & feedback properties
    [ObservableProperty] private bool _hasActiveKhatm;
    [ObservableProperty] private int _activeKhatmLastReadPage;
    [ObservableProperty] private int _currentVisiblePage;
    [ObservableProperty] private int _currentVisibleVerse = 1;
    [ObservableProperty] private bool _showHatimUpdateChip;
    [ObservableProperty] private string _hatimSummaryText = string.Empty;
    [ObservableProperty] private string _actionFeedbackMessage = string.Empty;

    public ObservableCollection<VerseItem> Verses { get; } = [];

    public event EventHandler? SurahNavigated;

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
        ShowTranslation = _keyValueStore.GetString(ShowTranslationKey) != "0";
        if (!ShowArabic && !ShowTranslation)
            ShowTranslation = true;

        if (double.TryParse(_keyValueStore.GetString(ArabicFontSizeKey), out var arSize))
            ArabicFontSize = Math.Clamp(arSize, 18, 40);
        else if (double.TryParse(_keyValueStore.GetString(FontScaleKey), out var scale))
            ArabicFontSize = Math.Clamp(Math.Round(24 * scale), 18, 40);

        if (double.TryParse(_keyValueStore.GetString(TranslationFontSizeKey), out var trSize))
            TranslationFontSize = Math.Clamp(trSize, 12, 26);
        else if (double.TryParse(_keyValueStore.GetString(FontScaleKey), out var scale))
            TranslationFontSize = Math.Clamp(Math.Round(15 * scale), 12, 26);
    }

    public async Task OnAppearingAsync()
    {
        await CheckHatimStatusAsync();

        if (Verses.Count > 0)
            return;

        await LoadSurahVersesAsync();
    }

    private async Task LoadSurahVersesAsync()
    {
        try
        {
            IsBusy = true;
            Verses.Clear();
            var verses = await _quranRepository.GetVersesAsync(SurahNumber);
            if (verses.Count == 0)
                return;

            SurahTitle = $"{verses[0].SurahName} Suresi";

            // Restore reading position if user didn't pass an explicit verse
            if (ScrollToVerse <= 0)
            {
                var savedForSurah = GetSurahSavedVerse(SurahNumber);
                if (savedForSurah > 1 && savedForSurah <= verses.Count)
                    ScrollToVerse = savedForSurah;
                else if (GetLastRead(_keyValueStore) is { } globalLast && globalLast.Surah == SurahNumber)
                    ScrollToVerse = Math.Clamp(globalLast.Verse, 1, verses.Count);
            }

            foreach (var verse in verses)
            {
                Verses.Add(new VerseItem
                {
                    Verse = verse,
                    IsFavorite = await _quranRepository.IsFavoriteAsync(verse.SurahNumber, verse.VerseNumber)
                });
            }

            var initialVerse = Math.Clamp(ScrollToVerse > 0 ? ScrollToVerse : 1, 1, verses.Count);
            var firstItem = Verses[initialVerse - 1].Verse;
            CurrentVisiblePage = firstItem.Page;
            CurrentVisibleVerse = firstItem.VerseNumber;
            UpdateSubtitle(verses.Count);
            SaveLastRead(initialVerse);
            UpdateHatimChipVisibility();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateSubtitle(int totalVerses = 0)
    {
        var count = totalVerses > 0 ? totalVerses : Verses.Count;
        SurahSubtitle = CurrentVisiblePage > 0
            ? $"Sayfa {CurrentVisiblePage} · Ayet {CurrentVisibleVerse}/{count}"
            : $"{count} ayet";
    }

    /// <summary>Called by the page as the user scrolls; remembers the reading position.</summary>
    public void OnVisibleVerseChanged(int visibleIndex)
    {
        if (visibleIndex >= 0 && visibleIndex < Verses.Count)
        {
            var verse = Verses[visibleIndex].Verse;
            CurrentVisibleVerse = verse.VerseNumber;
            SaveLastRead(verse.VerseNumber);
            if (verse.Page != CurrentVisiblePage)
            {
                CurrentVisiblePage = verse.Page;
                UpdateHatimChipVisibility();
            }
            UpdateSubtitle();
        }
    }

    private void SaveLastRead(int verseNumber)
    {
        if (SurahNumber > 0)
        {
            _keyValueStore.SetString(LastReadKey, $"{SurahNumber}:{verseNumber}");
            _keyValueStore.SetString($"quran_surah_pos_{SurahNumber}", verseNumber.ToString());
        }
    }

    private int GetSurahSavedVerse(int surahNumber)
    {
        var raw = _keyValueStore.GetString($"quran_surah_pos_{surahNumber}");
        return int.TryParse(raw, out var v) ? v : 1;
    }

    [RelayCommand]
    private void ToggleFocusMode()
    {
        IsFocusMode = !IsFocusMode;
        if (IsFocusMode)
            IsSettingsPanelOpen = false;
    }

    [RelayCommand]
    private void ToggleSettingsPanel() => IsSettingsPanelOpen = !IsSettingsPanelOpen;

    [RelayCommand]
    private void ToggleArabic()
    {
        if (ShowArabic && !ShowTranslation)
            ShowTranslation = true;

        ShowArabic = !ShowArabic;
        _keyValueStore.SetString(ShowArabicKey, ShowArabic ? "1" : "0");
        _keyValueStore.SetString(ShowTranslationKey, ShowTranslation ? "1" : "0");
    }

    [RelayCommand]
    private void ToggleTranslation()
    {
        if (ShowTranslation && !ShowArabic)
            ShowArabic = true;

        ShowTranslation = !ShowTranslation;
        _keyValueStore.SetString(ShowTranslationKey, ShowTranslation ? "1" : "0");
        _keyValueStore.SetString(ShowArabicKey, ShowArabic ? "1" : "0");
    }

    [RelayCommand]
    private void IncreaseArabicFont()
    {
        ArabicFontSize = Math.Clamp(ArabicFontSize + 2, 18, 40);
        _keyValueStore.SetString(ArabicFontSizeKey, ArabicFontSize.ToString("0"));
    }

    [RelayCommand]
    private void DecreaseArabicFont()
    {
        ArabicFontSize = Math.Clamp(ArabicFontSize - 2, 18, 40);
        _keyValueStore.SetString(ArabicFontSizeKey, ArabicFontSize.ToString("0"));
    }

    [RelayCommand]
    private void IncreaseTranslationFont()
    {
        TranslationFontSize = Math.Clamp(TranslationFontSize + 1, 12, 26);
        _keyValueStore.SetString(TranslationFontSizeKey, TranslationFontSize.ToString("0"));
    }

    [RelayCommand]
    private void DecreaseTranslationFont()
    {
        TranslationFontSize = Math.Clamp(TranslationFontSize - 1, 12, 26);
        _keyValueStore.SetString(TranslationFontSizeKey, TranslationFontSize.ToString("0"));
    }

    [RelayCommand]
    private void IncreaseFont()
    {
        IncreaseArabicFont();
        IncreaseTranslationFont();
    }

    [RelayCommand]
    private void DecreaseFont()
    {
        DecreaseArabicFont();
        DecreaseTranslationFont();
    }

    [RelayCommand]
    private async Task GoPreviousSurahAsync()
    {
        if (!CanGoPreviousSurah)
            return;

        await StopAudioAsync();
        SurahNumber--;
        ScrollToVerse = 1;
        await LoadSurahVersesAsync();
        SurahNavigated?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task GoNextSurahAsync()
    {
        if (!CanGoNextSurah)
            return;

        await StopAudioAsync();
        SurahNumber++;
        ScrollToVerse = 1;
        await LoadSurahVersesAsync();
        SurahNavigated?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(VerseItem item)
    {
        await _quranRepository.ToggleFavoriteAsync(item.Verse.SurahNumber, item.Verse.VerseNumber);
        item.IsFavorite = !item.IsFavorite;
        ShowFeedback(item.IsFavorite ? $"{item.Verse.Reference} favorilere eklendi" : "Favorilerden çıkarıldı");
    }

    [RelayCommand]
    private async Task CopyVerseAsync(VerseItem item)
    {
        var arabic = item.HasArabic && ShowArabic ? $"{item.ArabicText}\n\n" : string.Empty;
        await Clipboard.Default.SetTextAsync($"{arabic}\"{item.Text}\" — {item.Verse.Reference}");
        ShowFeedback($"{item.VerseNumber}. ayet panoya kopyalandı");
    }

    [RelayCommand]
    private static async Task ShareVerseAsync(VerseItem item)
    {
        var arabic = item.HasArabic ? $"{item.ArabicText}\n\n" : string.Empty;
        var text = $"{arabic}\"{item.Text}\"\n\n— {item.Verse.Reference}\n\nEzan Vakti uygulaması";
        await Share.Default.RequestAsync(new ShareTextRequest { Title = "Ayet Paylaş", Text = text });
    }

    private void ShowFeedback(string message)
    {
        ActionFeedbackMessage = message;
        _ = ClearFeedbackAsync();
    }

    private async Task ClearFeedbackAsync()
    {
        await Task.Delay(2200);
        ActionFeedbackMessage = string.Empty;
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

        return int.TryParse(raw, out var legacySurah) ? (legacySurah, 1) : null;
    }

    // ---- Audio Player ----

    [RelayCommand]
    private async Task PlayAudioAsync()
    {
        if (IsAudioLoading)
            return;

        try
        {
            AudioErrorMessage = string.Empty;
            IsAudioLoading = true;
            AudioStatusText = $"{SurahTitle} ses dosyası yükleniyor (internet gerekir)...";

            _audioPlayer.PlaybackEnded -= OnPlaybackEnded;
            _audioPlayer.PlaybackFailed -= OnPlaybackFailed;
            _audioPlayer.PlaybackEnded += OnPlaybackEnded;
            _audioPlayer.PlaybackFailed += OnPlaybackFailed;

            var url = $"https://download.quranicaudio.com/quran/mishaari_raashid_al_3afasy/{SurahNumber:D3}.mp3";
            await _audioPlayer.PlayAsync(url);
            IsPlaying = true;
            IsAudioPaused = false;
            AudioStatusText = $"{SurahTitle} · Mishary Rashid Al-Afasy";
        }
        catch
        {
            IsPlaying = false;
            IsAudioPaused = false;
            AudioErrorMessage = "Ses yüklenemedi. Sesli okuma için internet bağlantısı gerekir; Arapça metin ve meal çevrimdışı okunabilir.";
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
        IsAudioPaused = true;
        AudioStatusText = $"{SurahTitle} · Duraklatıldı";
    }

    [RelayCommand]
    private async Task StopAudioAsync()
    {
        await _audioPlayer.StopAsync();
        IsPlaying = false;
        IsAudioPaused = false;
        AudioErrorMessage = string.Empty;
    }

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsPlaying = false;
            IsAudioPaused = false;
            AudioStatusText = $"{SurahTitle} tilaveti tamamlandı";
        });
    }

    private void OnPlaybackFailed(object? sender, string error)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsPlaying = false;
            IsAudioPaused = false;
            AudioErrorMessage = "Ses oynatılırken bağlantı kesildi. İnternet bağlantınızı kontrol edip tekrar deneyin.";
        });
    }

    public void OnDisappearing()
    {
        _audioPlayer.PlaybackEnded -= OnPlaybackEnded;
        _audioPlayer.PlaybackFailed -= OnPlaybackFailed;
        _ = _audioPlayer.StopAsync();
        IsPlaying = false;
        IsAudioPaused = false;
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
                var target = plan.PagesPerDayTarget(_clock.Today);
                HatimSummaryText = target > 0
                    ? $"Hatim: {plan.LastReadPage}/604 sayfa · Günlük hedef {target} sayfa"
                    : $"Hatim: {plan.LastReadPage}/604 sayfa";
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
                await CheckHatimStatusAsync();
                ShowFeedback($"Hatim ilerlemeniz Sayfa {CurrentVisiblePage} olarak kaydedildi ✓");
            }
        }
        catch
        {
            ShowFeedback("Hatim ilerlemesi kaydedilemedi.");
        }
    }
}
