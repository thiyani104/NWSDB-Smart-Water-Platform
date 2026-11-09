using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NWSDB.API.Data;
using NWSDB.API.Data.Services;
using NWSDB.API.DTOs;
using NWSDB.API.Models;

namespace NWSDB.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PartnerController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _configuration;

    public PartnerController(
        AppDbContext context, 
        IPaymentService paymentService, 
        IConfiguration configuration)
    {
        _context = context;
        _paymentService = paymentService;
        _configuration = configuration;
    }

    private bool IsValidApiKey(string? apiKey)
    {
        var configuredKey = _configuration["PartnerConfig:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(configuredKey))
            return false;

        // Constant-time comparison to avoid leaking the key through timing differences
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(apiKey),
            Encoding.UTF8.GetBytes(configuredKey));
    }

    /// <summary>
    /// Third-party banking query endpoint for pending bills.
    /// Secured via X-API-KEY header validation.
    /// </summary>
    [HttpGet("bill/{accountNumber}")]
    public async Task<IActionResult> QueryBillByAccount(
        string accountNumber, 
        [FromHeader(Name = "X-API-KEY")] string? apiKey)
    {
        if (!IsValidApiKey(apiKey))
        {
            return Unauthorized(new { message = "Authentication failed. Invalid Third-Party Partner API Key." });
        }

        var bill = await _context.Bills
            .Where(b => b.AccountNumber == accountNumber && b.Status != "Paid" && b.OutstandingBalance > 0)
            .OrderByDescending(b => b.BillingDate)
            .FirstOrDefaultAsync();

        if (bill == null)
        {
            return NotFound(new { message = "No pending unpaid bill found for this account." });
        }

        return Ok(new
        {
            bill.Id,
            bill.AccountNumber,
            bill.Amount,
            OutstandingBalance = bill.OutstandingBalance,
            bill.DueDate,
            bill.Status
        });
    }

    /// <summary>
    /// B2B payment settlement endpoint for commercial banks and third-party partners.
    /// </summary>
    [HttpPost("pay")]
    public async Task<IActionResult> ProcessThirdPartyPayment(
        [FromBody] PaymentRequest request, 
        [FromHeader(Name = "X-API-KEY")] string? apiKey)
    {
        if (!IsValidApiKey(apiKey))
        {
            return Unauthorized(new { message = "Authentication failed. Invalid Third-Party Partner API Key." });
        }

        if (request == null)
        {
            return BadRequest(new { message = "Payment request body cannot be empty." });
        }

        request.PaymentMethod = "Partner_B2B_Settlement";

        try
        {
            var receipt = await _paymentService.ProcessPaymentAsync(request);
            if (receipt == null)
            {
                return NotFound(new { message = "Target bill record not found." });
            }

            _context.ActivityLogs.Add(new ActivityLog
            {
                PerformedBy = "PartnerGateway",
                Action = $"B2B payment of LKR {request.Amount:F2} processed for Bill ID {request.BillId}."
            });
            await _context.SaveChangesAsync();

            return Ok(receipt);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}