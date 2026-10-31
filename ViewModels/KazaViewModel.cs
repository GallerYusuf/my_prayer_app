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
/// Missed (kaza) prayer counters with single-action Undo recovery.
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
    private (string Key, string Name, int OldValue)? _lastUndo;

    [ObservableProperty] private string _totalText = string.Empty;
    [ObservableProperty] private bool _canUndo;
    [ObservableProperty] private string _undoMessage = string.Empty;

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

    [RelayCommand]
    private void Undo()
    {
        if (!CanUndo || _lastUndo is not { } undo)
            return;

        var item = Items.FirstOrDefault(i => i.Key == undo.Key);
        if (item is not null)
        {
            item.Count = undo.OldValue;
            Save();
            UpdateTotal();
        }

        CanUndo = false;
        _lastUndo = null;
    }

    private void Adjust(KazaItem item, int delta)
    {
        var oldVal = item.Count;
        var newVal = Math.Clamp(oldVal + delta, 0, 99_999);
        if (newVal == oldVal)
            return;

        _lastUndo = (item.Key, item.Name, oldVal);
        CanUndo = true;
        var diff = newVal - oldVal;
        UndoMessage = $"{item.Name}: {(diff > 0 ? $"+{diff}" : diff.ToString())} ({newVal})";

        item.Count = newVal;
        Save();
        UpdateTotal();
    }

    private void UpdateTotal()
    {
        var prayers = Items.Where(i => i.Key != "Fast").Sum(i => i.Count);
        var fasts = Items.FirstOrDefault(i => i.Key == "Fast")?.Count ?? 0;
        if (prayers == 0 && fasts == 0)
            TotalText = "Kayıtlı kaza borcunuz görünmüyor.";
        else if (fasts > 0)
            TotalText = $"Toplam {prayers} vakit namaz · {fasts} gün oruç kazası kayıtlı.";
        else
            TotalText = $"Toplam {prayers} vakit kaza namazı kayıtlı.";
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
