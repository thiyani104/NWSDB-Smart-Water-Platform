using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NWSDB.API.Data.Services;
using NWSDB.API.DTOs;

namespace NWSDB.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BillingController : ControllerBase
{
    private readonly IBillingService _billingService;
    private readonly IAccountAccessService _access;

    public BillingController(IBillingService billingService, IAccountAccessService access)
    {
        _billingService = billingService;
        _access = access;
    }

    /// <summary>
    /// Records a new meter reading and triggers automatic bill generation.
    /// Restricted to field staff and administrators.
    /// </summary>
    [Authorize(Roles = "Staff,Admin")]
    [HttpPost("meter-reading")]
    public async Task<IActionResult> RecordReading([FromBody] MeterReadingRequest request)
    {
        if (request == null)
            return BadRequest(new { message = "Meter reading payload cannot be empty." });

        // Audit trail integrity: the recording staff member comes from the JWT, not from the payload
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var staffId))
            return Unauthorized(new { message = "Unable to identify the authenticated staff member." });
        request.RecordedByStaffId = staffId;

        try
        {
            var result = await _billingService.RecordMeterReadingAsync(request);
            if (result == null)
                return NotFound(new { message = "Water connection not found for given account number." });

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            // Catches validation failures (e.g., Reading < PreviousReading or Reading < 0)
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves billing records for a specific water connection account.
    /// </summary>
    [HttpGet("bills/{accountNumber}")]
    public async Task<IActionResult> GetBills(string accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
            return BadRequest(new { message = "Account number is required." });

        if (!await _access.CanAccessAccountAsync(User, accountNumber))
            return Forbid();

        var bills = await _billingService.GetBillsByAccountAsync(accountNumber);
        return Ok(bills);
    }
}