using PrayerApp.Core.Services;
using Xunit;

namespace PrayerApp.UnitTests;

public class ZakatCalculatorTests
{
    [Fact]
    public void BelowNisab_NoZakatDue()
    {
        var result = ZakatCalculator.Calculate(
            goldGrams: 10, silverGrams: 0, cash: 1000, receivables: 0, debts: 0,
            goldPricePerGram: 3000, silverPricePerGram: 0);

        Assert.False(result.IsAboveNisab);
        Assert.Equal(0, result.ZakatDue);
        Assert.Equal(31_000, result.TotalWealth);
        Assert.Equal(80.18m * 3000, result.Nisab);
    }

    [Fact]
    public void AboveNisab_ChargesTwoPointFivePercent()
    {
        var result = ZakatCalculator.Calculate(
            goldGrams: 100, silverGrams: 0, cash: 100_000, receivables: 0, debts: 0,
            goldPricePerGram: 3000, silverPricePerGram: 0);

        Assert.True(result.IsAboveNisab);
        Assert.Equal(400_000, result.TotalWealth);
        Assert.Equal(10_000, result.ZakatDue);
    }

    [Fact]
    public void Debts_ReduceZakatableWealth()
    {
        var withDebt = ZakatCalculator.Calculate(0, 0, 500_000, 0, 200_000, 3000, 0);
        var withoutDebt = ZakatCalculator.Calculate(0, 0, 500_000, 0, 0, 3000, 0);

        Assert.Equal(300_000, withDebt.TotalWealth);
        Assert.True(withDebt.ZakatDue < withoutDebt.ZakatDue);
    }

    [Fact]
    public void ExactlyAtNisab_IsLiable()
    {
        var nisabCash = 80.18m * 3000;
        var result = ZakatCalculator.Calculate(0, 0, nisabCash, 0, 0, 3000, 0);

        Assert.True(result.IsAboveNisab);
        Assert.Equal(Math.Round(nisabCash * 0.025m, 2), result.ZakatDue);
    }

    [Fact]
    public void NegativeInputs_AreTreatedAsZero()
    {
        var result = ZakatCalculator.Calculate(-5, -1, -100, -20, -30, 3000, 100);

        Assert.Equal(0, result.TotalWealth);
        Assert.Equal(0, result.ZakatDue);
    }

    [Fact]
    public void DebtsExceedingWealth_ClampToZero()
    {
        var result = ZakatCalculator.Calculate(0, 0, 1000, 0, 5000, 3000, 0);

        Assert.Equal(0, result.TotalWealth);
        Assert.False(result.IsAboveNisab);
    }
}
