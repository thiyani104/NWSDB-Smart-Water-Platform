using Microsoft.EntityFrameworkCore;
using NWSDB.API.DTOs;
using NWSDB.API.Models;

namespace NWSDB.API.Data.Services;

public class BillingService : IBillingService
{
    private readonly AppDbContext _context;
    private readonly ITariffProvider _tariffProvider;

    public BillingService(AppDbContext context, ITariffProvider tariffProvider)
    {
        _context = context;
        _tariffProvider = tariffProvider;
    }

    public async Task<MeterReadingResponse?> RecordMeterReadingAsync(MeterReadingRequest request)
    {
        if (request.ReadingValue < 0)
            throw new ArgumentException("Meter reading cannot be negative.");

        var connection = await _context.WaterConnections
            .FirstOrDefaultAsync(c => c.AccountNumber == request.AccountNumber);

        if (connection == null) return null;

        // Retrieve the latest prior reading to calculate consumption
        var lastReading = await _context.MeterReadings
            .Where(m => m.WaterConnectionId == connection.Id)
            .OrderByDescending(m => m.ReadingDate)
            .FirstOrDefaultAsync();

        double previousValue = lastReading?.ReadingValue ?? 0;

        // Business rule: a new reading can never be lower than the previous one
        if (request.ReadingValue < previousValue)
        {
            throw new ArgumentException(
                $"New meter reading ({request.ReadingValue}) cannot be lower than previous reading ({previousValue}).");
        }

        double unitsUsed = request.ReadingValue - previousValue;

        // Tariff comes from the SystemConfiguration table (fixed charge + per-unit rate)
        var tariff = await _tariffProvider.GetCurrentTariffAsync();
        decimal billAmount = TariffCalculator.Calculate(unitsUsed, tariff);

        var reading = new MeterReading
        {
            WaterConnectionId = connection.Id,
            ReadingValue = request.ReadingValue,
            ReadingDate = DateTime.UtcNow,
            RecordedByStaffId = request.RecordedByStaffId
        };

        _context.MeterReadings.Add(reading);

        // Generate corresponding Bill with synchronized OutstandingBalance
        var bill = new Bill
        {
            AccountNumber = connection.AccountNumber,
            WaterConnectionId = connection.Id,
            BillingDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(tariff.GracePeriodDays),
            Amount = billAmount,
            OutstandingBalance = billAmount, // Initializes remaining balance to full amount
            Status = "Unpaid"
        };

        _context.Bills.Add(bill);

        // Log system activity
        _context.ActivityLogs.Add(new ActivityLog
        {
            PerformedBy = $"Staff #{request.RecordedByStaffId}",
            Action = $"Recorded reading {request.ReadingValue} for Account {connection.AccountNumber}. Bill generated: LKR {billAmount}."
        });

        await _context.SaveChangesAsync();

        return new MeterReadingResponse
        {
            Id = reading.Id,
            AccountNumber = connection.AccountNumber,
            ReadingValue = reading.ReadingValue,
            ReadingDate = reading.ReadingDate,
            RecordedByStaffId = reading.RecordedByStaffId,
            CalculatedBillAmount = billAmount
        };
    }

    public async Task<List<BillResponse>> GetBillsByAccountAsync(string accountNumber)
    {
        return await _context.Bills
            .Where(b => b.AccountNumber == accountNumber)
            .OrderByDescending(b => b.BillingDate)
            .Select(b => new BillResponse
            {
                Id = b.Id,
                AccountNumber = b.AccountNumber,
                BillingDate = b.BillingDate,
                DueDate = b.DueDate,
                Amount = b.Amount,
                Status = b.Status
            })
            .ToListAsync();
    }
}