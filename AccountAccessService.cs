using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace NWSDB.API.Data.Services;

public class AccountAccessService : IAccountAccessService
{
    private readonly AppDbContext _context;

    public AccountAccessService(AppDbContext context)
    {
        _context = context;
    }

    private static bool IsPrivileged(ClaimsPrincipal user) =>
        user.IsInRole("Admin") || user.IsInRole("Staff");

    private static bool TryGetUserId(ClaimsPrincipal user, out int userId) =>
        int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);

    public async Task<bool> CanAccessAccountAsync(ClaimsPrincipal user, string accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber)) return false;
        if (IsPrivileged(user)) return true;
        if (!TryGetUserId(user, out var userId)) return false;

        return await _context.WaterConnections
            .AnyAsync(c => c.AccountNumber == accountNumber && c.UserId == userId);
    }

    public async Task<bool> CanAccessBillAsync(ClaimsPrincipal user, int billId)
    {
        if (IsPrivileged(user)) return true;
        if (!TryGetUserId(user, out var userId)) return false;

        return await _context.Bills
            .AnyAsync(b => b.Id == billId && b.WaterConnection != null && b.WaterConnection.UserId == userId);
    }

    public async Task<bool> CanAccessPaymentAsync(ClaimsPrincipal user, int paymentId)
    {
        if (IsPrivileged(user)) return true;
        if (!TryGetUserId(user, out var userId)) return false;

        return await _context.Payments
            .AnyAsync(p => p.Id == paymentId
                           && p.Bill != null
                           && p.Bill.WaterConnection != null
                           && p.Bill.WaterConnection.UserId == userId);
    }
}
