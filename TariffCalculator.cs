namespace NWSDB.API.Data.Services;

/// <summary>Tariff values, normally loaded from the SystemConfiguration table.</summary>
public record Tariff(
    decimal BaseFixedCharge,
    decimal UnitRate,
    int GracePeriodDays,
    decimal LateFinePercentage)
{
    public static Tariff Default { get; } = new(300m, 50m, 30, 10m);
}

public static class TariffCalculator
{
    /// <summary>Bill = fixed monthly charge + (units consumed x unit rate), rounded to 2 decimals.</summary>
    public static decimal Calculate(double unitsUsed, Tariff tariff)
    {
        if (unitsUsed < 0)
            throw new ArgumentOutOfRangeException(nameof(unitsUsed), "Units used cannot be negative.");

        var amount = tariff.BaseFixedCharge + ((decimal)unitsUsed * tariff.UnitRate);
        return Math.Round(amount, 2, MidpointRounding.AwayFromZero);
    }
}
