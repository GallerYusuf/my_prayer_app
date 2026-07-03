using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrayerApp.Data;
using PrayerApp.Models;

namespace PrayerApp.ViewModels;

public enum PickerItemKind
{
    Province,
    WholeProvince,
    District
}

public partial class CityPickerViewModel : ObservableObject
{
    private readonly TaskCompletionSource<CitySelection?> _selectionTcs = new();
    private readonly IReadOnlyList<TurkishCity> _allCities = TurkishCities.GetAll();
    private readonly int _selectedPlateCode;
    private readonly string? _selectedDistrictApiName;

    private INavigation? _navigation;
    private TurkishCity? _activeCity;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _title = "Şehir Seç";

    [ObservableProperty]
    private string _subtitle = "Türkiye'nin 81 ili";

    [ObservableProperty]
    private string _resultSummary = "81 şehir";

    [ObservableProperty]
    private string _searchPlaceholder = "Şehir, plaka veya bölge ara...";

    [ObservableProperty]
    private string _backGlyph = "✕";

    [ObservableProperty]
    private bool _isDistrictMode;

    [ObservableProperty]
    private bool _hasNoResults;

    public ObservableCollection<PickerItem> Items { get; } = [];

    public CityPickerViewModel(int selectedPlateCode, string? selectedDistrictApiName)
    {
        _selectedPlateCode = selectedPlateCode;
        _selectedDistrictApiName = selectedDistrictApiName;
        ShowProvinces();
    }

    public void SetNavigation(INavigation navigation) => _navigation = navigation;

    public Task<CitySelection?> WaitForSelectionAsync() => _selectionTcs.Task;

    public async Task HandleItemAsync(PickerItem item)
    {
        switch (item.Kind)
        {
            case PickerItemKind.Province when item.City is not null:
                ShowDistricts(item.City);
                break;

            case PickerItemKind.WholeProvince when item.City is not null:
                await CompleteAsync(new CitySelection(item.City, null));
                break;

            case PickerItemKind.District when _activeCity is not null && item.District is not null:
                await CompleteAsync(new CitySelection(_activeCity, item.District));
                break;
        }
    }

    partial void OnSearchQueryChanged(string value) => ApplyFilter();

    private void ShowProvinces()
    {
        _activeCity = null;
        IsDistrictMode = false;
        Title = "Şehir Seç";
        Subtitle = "Türkiye'nin 81 ili";
        SearchPlaceholder = "Şehir, plaka veya bölge ara...";
        BackGlyph = "✕";
        SearchQuery = string.Empty;
        ApplyFilter();
    }

    private void ShowDistricts(TurkishCity city)
    {
        _activeCity = city;
        IsDistrictMode = true;
        Title = city.DisplayName;
        Subtitle = $"{city.Region} · {city.DistrictCount} ilçe";
        SearchPlaceholder = "İlçe ara...";
        BackGlyph = "‹";
        SearchQuery = string.Empty;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Items.Clear();

        if (IsDistrictMode && _activeCity is not null)
            PopulateDistricts(_activeCity);
        else
            PopulateProvinces();

        HasNoResults = Items.Count == 0;
    }

    private void PopulateProvinces()
    {
        var matches = _allCities.Where(c => TurkishCities.MatchesSearch(c, SearchQuery)).ToList();

        foreach (var city in matches)
        {
            Items.Add(new PickerItem(
                kind: PickerItemKind.Province,
                title: city.DisplayName,
                subtitle: $"{city.Region} · {city.DistrictCount} ilçe",
                badge: city.PlateLabel,
                showChevron: true,
                isSelected: city.PlateCode == _selectedPlateCode,
                city: city,
                district: null,
                onTap: HandleItemAsync));
        }

        ResultSummary = string.IsNullOrWhiteSpace(SearchQuery)
            ? "81 şehir"
            : $"{matches.Count} sonuç";
    }

    private void PopulateDistricts(TurkishCity city)
    {
        var hasQuery = !string.IsNullOrWhiteSpace(SearchQuery);

        if (!hasQuery)
        {
            Items.Add(new PickerItem(
                kind: PickerItemKind.WholeProvince,
                title: "Tüm İl (Merkez)",
                subtitle: "İl merkezine göre vakitler",
                badge: "★",
                showChevron: false,
                isSelected: city.PlateCode == _selectedPlateCode && string.IsNullOrEmpty(_selectedDistrictApiName),
                city: city,
                district: null,
                onTap: HandleItemAsync));
        }

        var matches = city.Districts.Where(d => TurkishCities.MatchesSearch(d, SearchQuery)).ToList();

        foreach (var district in matches)
        {
            Items.Add(new PickerItem(
                kind: PickerItemKind.District,
                title: district.DisplayName,
                subtitle: "İlçe",
                badge: "📍",
                showChevron: false,
                isSelected: city.PlateCode == _selectedPlateCode &&
                            string.Equals(district.ApiName, _selectedDistrictApiName, StringComparison.OrdinalIgnoreCase),
                city: city,
                district: district,
                onTap: HandleItemAsync));
        }

        ResultSummary = hasQuery ? $"{matches.Count} sonuç" : $"{city.DistrictCount} ilçe";
    }

    private Task CompleteAsync(CitySelection selection)
    {
        if (!_selectionTcs.Task.IsCompleted)
            _selectionTcs.TrySetResult(selection);

        // Defer the modal pop so the page isn't torn down while the tap gesture
        // is still being handled, which crashes WinUI with 0xC0000409.
        DispatchClose();
        return Task.CompletedTask;
    }

    private void DispatchClose()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null)
            dispatcher.Dispatch(async () => await CloseModalAsync());
        else
            _ = CloseModalAsync();
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        if (IsDistrictMode)
        {
            ShowProvinces();
            return;
        }

        await CancelAsync();
    }

    [RelayCommand]
    private Task CancelAsync()
    {
        if (!_selectionTcs.Task.IsCompleted)
            _selectionTcs.TrySetResult(null);

        DispatchClose();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void ClearSearch() => SearchQuery = string.Empty;

    private async Task CloseModalAsync()
    {
        var navigation = _navigation ?? Shell.Current?.Navigation;
        if (navigation is null)
            return;

        if (navigation.ModalStack.Count > 0)
            await navigation.PopModalAsync();
    }
}

public partial class PickerItem : ObservableObject
{
    private readonly Func<PickerItem, Task> _onTap;

    public PickerItemKind Kind { get; }
    public string Title { get; }
    public string Subtitle { get; }
    public string Badge { get; }
    public bool ShowChevron { get; }
    public TurkishCity? City { get; }
    public TurkishDistrict? District { get; }

    [ObservableProperty]
    private bool _isSelected;

    public PickerItem(
        PickerItemKind kind,
        string title,
        string subtitle,
        string badge,
        bool showChevron,
        bool isSelected,
        TurkishCity? city,
        TurkishDistrict? district,
        Func<PickerItem, Task> onTap)
    {
        Kind = kind;
        Title = title;
        Subtitle = subtitle;
        Badge = badge;
        ShowChevron = showChevron;
        IsSelected = isSelected;
        City = city;
        District = district;
        _onTap = onTap;
    }

    [RelayCommand]
    private Task Tap() => _onTap(this);
}
