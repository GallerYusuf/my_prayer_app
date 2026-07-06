using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PrayerApp.Data;

namespace PrayerApp.ViewModels;

/// <summary>The 99 names of Allah with search over transliteration and meaning.</summary>
public partial class EsmaulHusnaViewModel : ObservableObject
{
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _emptyMessage = string.Empty;

    public ObservableCollection<EsmaName> Names { get; } = [];

    public EsmaulHusnaViewModel() => ApplyFilter(string.Empty);

    partial void OnSearchTextChanged(string value) => ApplyFilter(value);

    private void ApplyFilter(string query)
    {
        var trimmed = query.Trim();
        var matches = trimmed.Length == 0
            ? EsmaulHusnaData.All
            : EsmaulHusnaData.All
                .Where(n =>
                    n.Transliteration.Contains(trimmed, StringComparison.CurrentCultureIgnoreCase) ||
                    n.Meaning.Contains(trimmed, StringComparison.CurrentCultureIgnoreCase))
                .ToList();

        Names.Clear();
        foreach (var name in matches)
            Names.Add(name);

        EmptyMessage = matches.Count == 0 ? "Aramanızla eşleşen isim bulunamadı." : string.Empty;
    }
}
