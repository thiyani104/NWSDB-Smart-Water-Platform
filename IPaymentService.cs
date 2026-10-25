using NWSDB.API.DTOs;

namespace NWSDB.API.Data.Services;

public interface IPaymentService
{
    Task<PaymentReceiptResponse?> ProcessPaymentAsync(PaymentRequest request);
    Task<PaymentReceiptResponse?> GetReceiptByPaymentIdAsync(int paymentId);
}