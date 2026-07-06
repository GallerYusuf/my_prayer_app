using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrayerApp.Core.Contracts;
using PrayerApp.Core.Services;

namespace PrayerApp.ViewModels;

/// <summary>
/// Offline zakat calculator. The user enters current gram prices manually
/// (no price feed — privacy-first, no network); inputs are remembered locally.
/// </summary>
public partial class ZakatViewModel : ObservableObject
{
    private const string GoldPriceKey = "zakat_gold_price";
    private const string SilverPriceKey = "zakat_silver_price";

    private static readonly CultureInfo TurkishCulture = new("tr-TR");

    private readonly IKeyValueStore _keyValueStore;

    [ObservableProperty] private string _goldGrams = string.Empty;
    [ObservableProperty] private string _silverGrams = string.Empty;
    [ObservableProperty] private string _cash = string.Empty;
    [ObservableProperty] private string _receivables = string.Empty;
    [ObservableProperty] private string _debts = string.Empty;
    [ObservableProperty] private string _goldPrice = string.Empty;
    [ObservableProperty] private string _silverPrice = string.Empty;

    [ObservableProperty] private bool _hasResult;
    [ObservableProperty] private string _resultTitle = string.Empty;
    [ObservableProperty] private string _resultDetail = string.Empty;
    [ObservableProperty] private string _validationMessage = string.Empty;

    public ZakatViewModel(IKeyValueStore keyValueStore)
    {
        _keyValueStore = keyValueStore;
        GoldPrice = _keyValueStore.GetString(GoldPriceKey) ?? string.Empty;
        SilverPrice = _keyValueStore.GetString(SilverPriceKey) ?? string.Empty;
    }

    [RelayCommand]
    private void Calculate()
    {
        ValidationMessage = string.Empty;
        HasResult = false;

        if (!TryParse(GoldPrice, out var goldPrice) || goldPrice <= 0)
        {
            ValidationMessage = "Hesap için güncel gram altın fiyatını girin (nisap altına göre belirlenir).";
            return;
        }

        if (!TryParse(GoldGrams, out var goldGrams) ||
            !TryParse(SilverGrams, out var silverGrams) ||
            !TryParse(Cash, out var cash) ||
            !TryParse(Receivables, out var receivables) ||
            !TryParse(Debts, out var debts) ||
            !TryParse(SilverPrice, out var silverPrice))
        {
            ValidationMessage = "Sayısal alanları kontrol edin; boş alanlar 0 kabul edilir.";
            return;
        }

        if (silverGrams > 0 && silverPrice <= 0)
        {
            ValidationMessage = "Gümüşünüz varsa gram gümüş fiyatını da girin.";
            return;
        }

        _keyValueStore.SetString(GoldPriceKey, GoldPrice.Trim());
        if (!string.IsNullOrWhiteSpace(SilverPrice))
            _keyValueStore.SetString(SilverPriceKey, SilverPrice.Trim());

        var result = ZakatCalculator.Calculate(
            goldGrams, silverGrams, cash, receivables, debts, goldPrice, silverPrice);

        HasResult = true;
        if (result.IsAboveNisab)
        {
            ResultTitle = $"Zekât: {result.ZakatDue.ToString("N2", TurkishCulture)} TL";
            ResultDetail = $"Zekâta tabi servet: {result.TotalWealth.ToString("N2", TurkishCulture)} TL · " +
                           $"Nisap: {result.Nisab.ToString("N2", TurkishCulture)} TL. " +
                           "Servetin üzerinden bir kameri yıl geçmiş olması gerekir.";
        }
        else
        {
            ResultTitle = "Zekât gerekmiyor";
            ResultDetail = $"Servetiniz ({result.TotalWealth.ToString("N2", TurkishCulture)} TL) " +
                           $"nisap miktarının ({result.Nisab.ToString("N2", TurkishCulture)} TL) altında.";
        }
    }

    private static bool TryParse(string? text, out decimal value)
    {
        var trimmed = text?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            value = 0;
            return true;
        }

        return decimal.TryParse(trimmed, NumberStyles.Number, TurkishCulture, out value)
            || decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}
