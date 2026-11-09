using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NWSDB.API.Data;
using NWSDB.API.Data.Services;
using NWSDB.API.DTOs;
using NWSDB.API.Models;

namespace NWSDB.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuthService _authService;
    private readonly ITariffProvider _tariffProvider;

    public AdminController(AppDbContext context, IAuthService authService, ITariffProvider tariffProvider)
    {
        _context = context;
        _authService = authService;
        _tariffProvider = tariffProvider;
    }

    /// <summary>
    /// Provision a new internal Staff account (Restricted strictly to Administrator).
    /// </summary>
    [HttpPost("register-staff")]
    public async Task<IActionResult> RegisterStaff([FromBody] RegisterRequest request)
    {
        if (request == null)
            return BadRequest(new { message = "Registration details cannot be empty." });

        // The Staff role is enforced server-side; the payload has no say in it.
        var result = await _authService.RegisterAsync(request, "Staff");
        if (result == null)
        {
            return BadRequest(new { message = "A user with this username or email already exists." });
        }

        var adminName = User.Identity?.Name ?? "Administrator";

        // Record the event in the security audit trail
        _context.ActivityLogs.Add(new ActivityLog
        {
            PerformedBy = adminName,
            Action = $"Admin provisioned new Staff account for '{request.Username}' ({request.Email})."
        });
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Staff member provisioned successfully.",
            userId = result.Id,
            username = result.Username,
            email = result.Email,
            role = "Staff"
        });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.Users
            .Select(u => new { u.Id, u.Username, u.Email, u.Phone, u.Role })
            .ToListAsync();
        return Ok(users);
    }

    [HttpPut("assign-role/{userId}")]
    public async Task<IActionResult> AssignRole(int userId, [FromBody] string newRole)
    {
        if (string.IsNullOrWhiteSpace(newRole))
            return BadRequest(new { message = "Role cannot be empty." });

        var validRoles = new[] { "Customer", "Staff", "Admin", "Partner" };
        if (!validRoles.Contains(newRole, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { message = $"Invalid role. Allowed roles: {string.Join(", ", validRoles)}" });

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound(new { message = "User not found." });

        string oldRole = user.Role;
        user.Role = newRole;

        // Retrieve the authenticated administrator's username from JWT claims
        var adminName = User.Identity?.Name ?? "Administrator";

        _context.ActivityLogs.Add(new ActivityLog
        {
            PerformedBy = adminName,
            Action = $"Changed role for user '{user.Username}' from '{oldRole}' to '{newRole}'."
        });

        await _context.SaveChangesAsync();
        return Ok(new { message = $"Role successfully updated to {newRole}." });
    }

    [HttpGet("activity-logs")]
    public async Task<IActionResult> GetActivityLogs()
    {
        var logs = await _context.ActivityLogs
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync();
        return Ok(logs);
    }

    [HttpGet("reports/summary")]
    public async Task<IActionResult> GetSystemSummary()
    {
        var totalUsers = await _context.Users.CountAsync();
        var totalConnections = await _context.WaterConnections.CountAsync();
        var totalBilled = await _context.Bills.SumAsync(b => b.Amount);
        var totalCollected = await _context.Payments
            .Where(p => p.PaymentStatus == "Success" || p.PaymentStatus == "Completed")
            .SumAsync(p => p.Amount);

        return Ok(new
        {
            TotalRegisteredUsers = totalUsers,
            ActiveConnections = totalConnections,
            TotalRevenueBilledLKR = totalBilled,
            TotalRevenueCollectedLKR = totalCollected,
            PendingCollectionLKR = totalBilled - totalCollected
        });
    }

    [HttpPost("database/backup")]
    public IActionResult BackupDatabase()
    {
        var adminName = User.Identity?.Name ?? "Administrator";

        _context.ActivityLogs.Add(new ActivityLog
        {
            PerformedBy = adminName,
            Action = "Simulated administrative database backup snapshot trigger."
        });
        _context.SaveChanges();

        return Ok(new 
        { 
            message = "Backup operation executed successfully (Simulated Prototype Mode).", 
            backupLocation = "C:\\backups\\NWSDB_SmartWater_Backup.bak",
            timestamp = DateTime.UtcNow,
            isSimulated = true
        });
    }

    [HttpPost("database/restore")]
    public IActionResult RestoreDatabase()
    {
        var adminName = User.Identity?.Name ?? "Administrator";

        _context.ActivityLogs.Add(new ActivityLog
        {
            PerformedBy = adminName,
            Action = "Simulated administrative database restore snapshot trigger."
        });
        _context.SaveChanges();

        return Ok(new 
        { 
            message = "Database restored successfully from last snapshot (Simulated Prototype Mode).",
            timestamp = DateTime.UtcNow,
            isSimulated = true
        });
    }

    [HttpGet("configuration/tariffs")]
    public async Task<IActionResult> GetSystemTariffs()
    {
        var tariff = await _tariffProvider.GetCurrentTariffAsync();
        return Ok(new
        {
            BaseFixedChargeLKR = tariff.BaseFixedCharge,
            UnitRateLKR = tariff.UnitRate,
            LatePaymentFineRatePercent = tariff.LateFinePercentage,
            GracePeriodDays = tariff.GracePeriodDays
        });
    }
}
