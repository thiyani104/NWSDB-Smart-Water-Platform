using Microsoft.EntityFrameworkCore;
using NWSDB.API.DTOs;

namespace NWSDB.API.Data.Services;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _context;

    public CustomerService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<WaterConnectionResponse?> GetConnectionByAccountAsync(string accountNumber)
    {
        var connection = await _context.WaterConnections
            .FirstOrDefaultAsync(c => c.AccountNumber == accountNumber);

        if (connection == null) return null;

        return new WaterConnectionResponse
        {
            Id = connection.Id,
            AccountNumber = connection.AccountNumber,
            ConnectionAddress = connection.ConnectionAddress,
            ConnectionStatus = connection.ConnectionStatus,
            UserId = connection.UserId
        };
    }

    public async Task<List<WaterConnectionResponse>> GetConnectionsByUserIdAsync(int userId)
    {
        return await _context.WaterConnections
            .Where(c => c.UserId == userId)
            .Select(c => new WaterConnectionResponse
            {
                Id = c.Id,
                AccountNumber = c.AccountNumber,
                ConnectionAddress = c.ConnectionAddress,
                ConnectionStatus = c.ConnectionStatus,
                UserId = c.UserId
            })
            .ToListAsync();
    }

    public async Task<BillResponse?> GetCurrentBillAsync(string accountNumber)
    {
        var bill = await _context.Bills
            .Where(b => b.AccountNumber == accountNumber)
            .OrderByDescending(b => b.BillingDate)
            .FirstOrDefaultAsync();

        if (bill == null) return null;

        return new BillResponse
        {
            Id = bill.Id,
            AccountNumber = bill.AccountNumber,
            BillingDate = bill.BillingDate,
            DueDate = bill.DueDate,
            Amount = bill.Amount,
            Status = bill.Status
        };
    }
}