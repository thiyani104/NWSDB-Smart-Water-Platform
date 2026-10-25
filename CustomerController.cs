using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NWSDB.API.Data;
using NWSDB.API.Data.Services;
using NWSDB.API.Models;

namespace NWSDB.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly AppDbContext _context;
    private readonly IAccountAccessService _access;

    public CustomerController(ICustomerService customerService, AppDbContext context, IAccountAccessService access)
    {
        _customerService = customerService;
        _context = context;
        _access = access;
    }

    // Helper to get authenticated User ID from claims
    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idClaim, out int id) ? id : 0;
    }

    [HttpGet("connection/{accountNumber}")]
    public async Task<IActionResult> GetConnection(string accountNumber)
    {
        if (!await _access.CanAccessAccountAsync(User, accountNumber))
            return Forbid();

        var connection = await _customerService.GetConnectionByAccountAsync(accountNumber);
        if (connection == null) return NotFound(new { message = "Water connection not found." });
        return Ok(connection);
    }

    [HttpGet("user/{userId}/connections")]
    public async Task<IActionResult> GetUserConnections(int userId)
    {
        int currentUserId = GetCurrentUserId();
        // Only allow if the requesting user is the owner, or is Staff/Admin
        if (currentUserId != userId && !User.IsInRole("Admin") && !User.IsInRole("Staff"))
        {
            return Forbid();
        }

        var connections = await _customerService.GetConnectionsByUserIdAsync(userId);
        return Ok(connections);
    }

    // Convenience endpoint: Get current user's connections directly from token
    [HttpGet("my-connections")]
    public async Task<IActionResult> GetMyConnections()
    {
        int currentUserId = GetCurrentUserId();
        var connections = await _customerService.GetConnectionsByUserIdAsync(currentUserId);
        return Ok(connections);
    }

    // View Current Bill + Overdue Warning & Disconnection Warning
    [HttpGet("bill/{accountNumber}")]
    public async Task<IActionResult> GetCurrentBill(string accountNumber)
    {
        if (!await _access.CanAccessAccountAsync(User, accountNumber))
            return Forbid();

        var bill = await _context.Bills
            .Where(b => b.AccountNumber == accountNumber)
            .OrderByDescending(b => b.BillingDate)
            .FirstOrDefaultAsync();

        if (bill == null) return NotFound(new { message = "No bill found." });

        bool isOverdue = bill.Status == "Unpaid" && DateTime.UtcNow > bill.DueDate;
        bool isDisconnectionWarning = isOverdue && (DateTime.UtcNow - bill.DueDate).TotalDays > 14;

        return Ok(new
        {
            bill.Id,
            bill.AccountNumber,
            bill.BillingDate,
            bill.DueDate,
            bill.Amount,
            bill.Status,
            IsOverdue = isOverdue,
            IsDisconnectionWarning = isDisconnectionWarning,
            WarningMessage = isDisconnectionWarning 
                ? "WARNING: Water disconnection order pending due to overdue bill payment!" 
                : isOverdue ? "Notice: Bill payment is past due date." : "Account in good standing."
        });
    }

    // GET: Current authenticated user's profile
    [HttpGet("profile/me")]
    public async Task<IActionResult> GetMyProfile()
    {
        int currentUserId = GetCurrentUserId();
        return await GetProfile(currentUserId);
    }

    // GET: Pre-populate Customer Profile details with authorization check
    [HttpGet("profile/{userId}")]
    public async Task<IActionResult> GetProfile(int userId)
    {
        int currentUserId = GetCurrentUserId();
        if (currentUserId != userId && !User.IsInRole("Admin") && !User.IsInRole("Staff"))
        {
            return Forbid();
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound(new { message = "User not found." });

        return Ok(new
        {
            user.Id,
            user.Username,
            user.Email,
            user.Phone,
            user.Role
        });
    }

    // PUT: Manage current user's profile directly
    [HttpPut("profile/me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] ProfileUpdateRequest request)
    {
        int currentUserId = GetCurrentUserId();
        return await UpdateProfile(currentUserId, request);
    }

    // PUT: Manage Profile (Contact details & Optional Password change)
    [HttpPut("profile/{userId}")]
    public async Task<IActionResult> UpdateProfile(int userId, [FromBody] ProfileUpdateRequest request)
    {
        int currentUserId = GetCurrentUserId();
        if (currentUserId != userId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound(new { message = "User not found." });

        user.Email = request.Email.Trim();
        user.Phone = request.Phone.Trim();

        // Optional password update logic
        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword) || 
                !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return BadRequest(new { message = "Current password does not match." });
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        }

        await _context.SaveChangesAsync();

        return Ok(new 
        { 
            message = "Profile successfully updated.", 
            user.Username,
            user.Email, 
            user.Phone 
        });
    }

    // View Current Usage and Usage History
    [HttpGet("usage/{accountNumber}")]
    public async Task<IActionResult> GetUsageHistory(string accountNumber)
    {
        if (!await _access.CanAccessAccountAsync(User, accountNumber))
            return Forbid();

        var history = await _context.MeterReadings
            .Include(m => m.WaterConnection)
            .Where(m => m.WaterConnection!.AccountNumber == accountNumber)
            .OrderByDescending(m => m.ReadingDate)
            .Select(m => new { m.ReadingDate, m.ReadingValue })
            .ToListAsync();

        return Ok(history);
    }

    // View Payment History
    [HttpGet("payment-history/{accountNumber}")]
    public async Task<IActionResult> GetPaymentHistory(string accountNumber)
    {
        if (!await _access.CanAccessAccountAsync(User, accountNumber))
            return Forbid();

        var payments = await _context.Payments
            .Include(p => p.Bill)
            .Include(p => p.Receipt)
            .Where(p => p.Bill!.AccountNumber == accountNumber)
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new
            {
                p.Id,
                p.Amount,
                p.PaymentDate,
                p.PaymentMethod,
                p.PaymentStatus,
                ReceiptNumber = p.Receipt != null ? p.Receipt.ReceiptNumber : "N/A"
            })
            .ToListAsync();

        return Ok(payments);
    }

    // Submit Feedback (bound to a DTO so clients cannot set Id, Status or UserId)
    [HttpPost("feedback")]
    public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { message = "Feedback message is required." });

        if (!await _access.CanAccessAccountAsync(User, request.AccountNumber))
            return Forbid();

        var feedback = new Feedback
        {
            UserId = GetCurrentUserId(),
            AccountNumber = request.AccountNumber,
            Message = request.Message.Trim(),
            SubmittedAt = DateTime.UtcNow,
            Status = "Pending"
        };

        _context.Feedbacks.Add(feedback);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Customer feedback submitted successfully.", feedback.Id });
    }
}

public class FeedbackRequest
{
    public string AccountNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class ProfileUpdateRequest
{
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
}