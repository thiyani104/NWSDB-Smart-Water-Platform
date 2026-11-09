using Microsoft.EntityFrameworkCore;
using NWSDB.API.DTOs;
using NWSDB.API.Models;

namespace NWSDB.API.Data.Services;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _context;

    public PaymentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentReceiptResponse?> ProcessPaymentAsync(PaymentRequest request)
    {
        // 1. Locate the bill
        var bill = await _context.Bills.FirstOrDefaultAsync(b => b.Id == request.BillId);
        if (bill == null) return null;

        // 2. BUSINESS RULE: Prevent paying an already settled bill
        if (bill.Status == "Paid" || bill.OutstandingBalance <= 0)
        {
            throw new InvalidOperationException("This bill has already been fully paid and settled.");
        }

        // 3. BUSINESS RULE: Validate amount constraints (No zero/negative payment, No overpayment)
        if (request.Amount <= 0)
        {
            throw new ArgumentException("Payment amount must be greater than zero.");
        }

        // If your business rule requires exact full settlement:
        if (request.Amount != bill.OutstandingBalance)
        {
            throw new InvalidOperationException(
                $"Invalid payment amount. The exact outstanding balance of LKR {bill.OutstandingBalance:F2} is required.");
        }

        // 4. Create the payment record
        var payment = new Payment
        {
            BillId = bill.Id,
            Amount = request.Amount,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "Online_Card" : request.PaymentMethod,
            PaymentStatus = "Success"
        };

        _context.Payments.Add(payment);

        // 5. Update OutstandingBalance and Bill Status
        bill.OutstandingBalance -= request.Amount;
        if (bill.OutstandingBalance <= 0)
        {
            bill.OutstandingBalance = 0;
            bill.Status = "Paid";
        }
        else
        {
            bill.Status = "Partially Paid";
        }

        // 6. Generate the official Payment Receipt
        var receipt = new PaymentReceipt
        {
            Payment = payment,
            ReceiptNumber = $"REC-{DateTime.UtcNow:yyyyMMddHHmmss}-{bill.Id}",
            ReceiptDate = DateTime.UtcNow
        };

        _context.PaymentReceipts.Add(receipt);

        // 7. Audit Log Entry
        _context.ActivityLogs.Add(new ActivityLog
        {
            PerformedBy = $"Account: {bill.AccountNumber}",
            Action = $"Payment of LKR {payment.Amount:F2} processed via {payment.PaymentMethod}. Receipt issued: {receipt.ReceiptNumber}."
        });

        await _context.SaveChangesAsync();

        return new PaymentReceiptResponse
        {
            PaymentId = payment.Id,
            ReceiptNumber = receipt.ReceiptNumber,
            ReceiptDate = receipt.ReceiptDate,
            AmountPaid = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            PaymentStatus = payment.PaymentStatus,
            AccountNumber = bill.AccountNumber,
            BillStatus = bill.Status
        };
    }

    public async Task<PaymentReceiptResponse?> GetReceiptByPaymentIdAsync(int paymentId)
    {
        var payment = await _context.Payments
            .Include(p => p.Bill)
            .Include(p => p.Receipt)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null || payment.Receipt == null || payment.Bill == null)
            return null;

        return new PaymentReceiptResponse
        {
            PaymentId = payment.Id,
            ReceiptNumber = payment.Receipt.ReceiptNumber,
            ReceiptDate = payment.Receipt.ReceiptDate,
            AmountPaid = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            PaymentStatus = payment.PaymentStatus,
            AccountNumber = payment.Bill.AccountNumber,
            BillStatus = payment.Bill.Status
        };
    }
}