using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace NWSDB.API.Data.Services;

public interface ITariffProvider
{
    Task<Tariff> GetCurrentTariffAsync();
}

/// <summary>Reads tariffs from the SystemConfiguration table, falling back to defaults per key.</summary>
public class TariffProvider : ITariffProvider
{
    private readonly AppDbContext _context;

    public TariffProvider(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Tariff> GetCurrentTariffAsync()
    {
        var settings = await _context.SystemConfigurations
            .AsNoTracking()
            .ToDictionaryAsync(c => c.ConfigKey, c => c.ConfigValue);

        var d = Tariff.Default;
        return new Tariff(
            ReadDecimal(settings, "Tariff_BaseFixedCharge", d.BaseFixedCharge),
            ReadDecimal(settings, "Tariff_UnitRate", d.UnitRate),
            (int)ReadDecimal(settings, "Billing_GracePeriodDays", d.GracePeriodDays),
            ReadDecimal(settings, "Billing_LateFinePercentage", d.LateFinePercentage));
    }

    private static decimal ReadDecimal(Dictionary<string, string> settings, string key, decimal fallback) =>
        settings.TryGetValue(key, out var raw)
        && decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
}
