using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NWSDB.API.Data.Services;
using NWSDB.API.DTOs;

namespace NWSDB.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IAccountAccessService _access;

    public PaymentController(IPaymentService paymentService, IAccountAccessService access)
    {
        _paymentService = paymentService;
        _access = access;
    }

    /// <summary>
    /// Processes online bill settlement and issues a transaction receipt.
    /// Guarded against overpayment, zero amounts, and paying settled bills.
    /// </summary>
    [HttpPost("pay")]
    public async Task<IActionResult> MakePayment([FromBody] PaymentRequest request)
    {
        if (request == null)
            return BadRequest(new { message = "Payment payload cannot be empty." });

        // A customer may only pay bills that belong to one of their own connections
        if (!await _access.CanAccessBillAsync(User, request.BillId))
            return Forbid();

        try
        {
            var receipt = await _paymentService.ProcessPaymentAsync(request);
            if (receipt == null)
                return NotFound(new { message = "Bill not found." });

            return Ok(receipt);
        }
        catch (InvalidOperationException ex)
        {
            // Catches already settled bills, status mismatches, or overpayment attempts
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            // Catches invalid/negative amounts
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a verifiable receipt by payment identifier.
    /// </summary>
    [HttpGet("receipt/{paymentId}")]
    public async Task<IActionResult> GetReceipt(int paymentId)
    {
        if (!await _access.CanAccessPaymentAsync(User, paymentId))
            return Forbid();

        var receipt = await _paymentService.GetReceiptByPaymentIdAsync(paymentId);
        if (receipt == null)
            return NotFound(new { message = "Receipt not found for this payment ID." });

        return Ok(receipt);
    }
}