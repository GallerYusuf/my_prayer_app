namespace PrayerApp.Core.Services;

public sealed record ZakatResult(
    decimal TotalWealth,
    decimal Nisab,
    bool IsAboveNisab,
    decimal ZakatDue);

/// <summary>
/// Pure zakat calculation. Nisab follows the gold standard used by Diyanet
/// (80.18 g of gold); zakat is 2.5% of net zakatable wealth once it meets nisab.
/// The one-lunar-year possession condition is the user's responsibility and is
/// stated in the UI, not modelled here.
/// </summary>
public static class ZakatCalculator
{
    public const decimal NisabGoldGrams = 80.18m;
    public const decimal Rate = 0.025m;

    public static ZakatResult Calculate(
        decimal goldGrams,
        decimal silverGrams,
        decimal cash,
        decimal receivables,
        decimal debts,
        decimal goldPricePerGram,
        decimal silverPricePerGram)
    {
        goldGrams = Math.Max(0, goldGrams);
        silverGrams = Math.Max(0, silverGrams);
        cash = Math.Max(0, cash);
        receivables = Math.Max(0, receivables);
        debts = Math.Max(0, debts);
        goldPricePerGram = Math.Max(0, goldPricePerGram);
        silverPricePerGram = Math.Max(0, silverPricePerGram);

        var wealth = goldGrams * goldPricePerGram
                   + silverGrams * silverPricePerGram
                   + cash
                   + receivables
                   - debts;

        if (wealth < 0)
            wealth = 0;

        var nisab = NisabGoldGrams * goldPricePerGram;
        var aboveNisab = nisab > 0 && wealth >= nisab;

        return new ZakatResult(
            TotalWealth: Math.Round(wealth, 2),
            Nisab: Math.Round(nisab, 2),
            IsAboveNisab: aboveNisab,
            ZakatDue: aboveNisab ? Math.Round(wealth * Rate, 2) : 0);
    }
}
