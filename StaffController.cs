using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NWSDB.API.Data;
using NWSDB.API.Models;

namespace NWSDB.API.Controllers;

[Authorize(Roles = "Staff,Admin")]
[ApiController]
[Route("api/[controller]")]
public class StaffController : ControllerBase
{
    private readonly AppDbContext _context;

    public StaffController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("customers")]
    public async Task<IActionResult> GetAllCustomers()
    {
        var customers = await _context.Users
            .Where(u => u.Role == "Customer")
            .Select(u => new { u.Id, u.Username, u.Email, u.Phone })
            .ToListAsync();

        return Ok(customers);
    }

    [HttpPost("connection")]
    public async Task<IActionResult> AddConnection([FromBody] WaterConnection connection)
    {
        if (connection == null)
            return BadRequest(new { message = "Connection data cannot be empty." });

        _context.WaterConnections.Add(connection);

        var staffName = User.Identity?.Name ?? "StaffMember";
        _context.ActivityLogs.Add(new ActivityLog
        {
            PerformedBy = staffName,
            Action = $"Registered new water connection '{connection.AccountNumber}' for UserId {connection.UserId}."
        });

        await _context.SaveChangesAsync();
        return Ok(connection);
    }

    [HttpPut("connection/{id}/status")]
    public async Task<IActionResult> UpdateConnectionStatus(int id, [FromBody] string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return BadRequest(new { message = "Status cannot be empty." });

        var conn = await _context.WaterConnections.FindAsync(id);
        if (conn == null) return NotFound(new { message = "Connection not found." });

        string oldStatus = conn.ConnectionStatus;
        conn.ConnectionStatus = status;

        var staffName = User.Identity?.Name ?? "StaffMember";
        _context.ActivityLogs.Add(new ActivityLog
        {
            PerformedBy = staffName,
            Action = $"Updated status for connection '{conn.AccountNumber}' from '{oldStatus}' to '{status}'."
        });

        await _context.SaveChangesAsync();
        return Ok(new { message = "Connection status updated.", conn.AccountNumber, conn.ConnectionStatus });
    }

    [HttpGet("payments")]
    public async Task<IActionResult> GetAllPayments()
    {
        var payments = await _context.Payments
            .Include(p => p.Bill)
            .Include(p => p.Receipt)
            .Select(p => new
            {
                p.Id,
                p.Bill!.AccountNumber,
                p.Amount,
                p.PaymentDate,
                p.PaymentMethod,
                p.PaymentStatus,
                ReceiptNumber = p.Receipt != null ? p.Receipt.ReceiptNumber : null
            })
            .ToListAsync();

        return Ok(payments);
    }

    [HttpGet("feedbacks")]
    public async Task<IActionResult> GetAllFeedbacks()
    {
        var feedbacks = await _context.Feedbacks
            .OrderByDescending(f => f.SubmittedAt)
            .ToListAsync();
        return Ok(feedbacks);
    }

    [HttpPut("feedback/{id}/review")]
    public async Task<IActionResult> ReviewFeedback(int id)
    {
        var fb = await _context.Feedbacks.FindAsync(id);
        if (fb == null) return NotFound(new { message = "Feedback not found." });

        fb.Status = "Reviewed";

        var staffName = User.Identity?.Name ?? "StaffMember";
        _context.ActivityLogs.Add(new ActivityLog
        {
            PerformedBy = staffName,
            Action = $"Reviewed feedback ticket #{id}."
        });

        await _context.SaveChangesAsync();
        return Ok(new { message = "Feedback marked as reviewed." });
    }
}