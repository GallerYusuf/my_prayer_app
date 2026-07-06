using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrayerApp.Core.Contracts;

namespace PrayerApp.ViewModels;

public partial class KazaItem : ObservableObject
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Icon { get; init; }

    [ObservableProperty] private int _count;
}

/// <summary>
/// Missed (kaza) prayer counters. Users track their own debt per prayer and
/// decrease it as they make up prayers; data stays on the device.
/// </summary>
public partial class KazaViewModel : ObservableObject
{
    private const string StorageKey = "kaza_counters";

    private static readonly (string Key, string Name, string Icon)[] Definitions =
    [
        ("Fajr", "Sabah", "icon_fajr.png"),
        ("Dhuhr", "Öğle", "icon_dhuhr.png"),
        ("Asr", "İkindi", "icon_asr.png"),
        ("Maghrib", "Akşam", "icon_maghrib.png"),
        ("Isha", "Yatsı", "icon_isha.png"),
        ("Witr", "Vitir", "icon_isha.png"),
        ("Fast", "Oruç", "icon_crescent.png")
    ];

    private readonly IKeyValueStore _keyValueStore;

    [ObservableProperty] private string _totalText = string.Empty;

    public ObservableCollection<KazaItem> Items { get; } = [];

    public KazaViewModel(IKeyValueStore keyValueStore)
    {
        _keyValueStore = keyValueStore;

        var counts = Load();
        foreach (var (key, name, icon) in Definitions)
            Items.Add(new KazaItem { Key = key, Name = name, Icon = icon, Count = counts.GetValueOrDefault(key) });

        UpdateTotal();
    }

    [RelayCommand]
    private void Increment(KazaItem item) => Adjust(item, +1);

    [RelayCommand]
    private void Decrement(KazaItem item) => Adjust(item, -1);

    [RelayCommand]
    private void IncrementTen(KazaItem item) => Adjust(item, +10);

    [RelayCommand]
    private void DecrementTen(KazaItem item) => Adjust(item, -10);

    private void Adjust(KazaItem item, int delta)
    {
        item.Count = Math.Clamp(item.Count + delta, 0, 99_999);
        Save();
        UpdateTotal();
    }

    private void UpdateTotal()
    {
        var prayers = Items.Where(i => i.Key != "Fast").Sum(i => i.Count);
        TotalText = prayers == 0
            ? "Kaza borcunuz görünmüyor."
            : $"Toplam {prayers} vakit kaza borcu kayıtlı.";
    }

    private Dictionary<string, int> Load()
    {
        var json = _keyValueStore.GetString(StorageKey);
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private void Save()
    {
        var counts = Items.ToDictionary(i => i.Key, i => i.Count);
        _keyValueStore.SetString(StorageKey, JsonSerializer.Serialize(counts));
    }
}
