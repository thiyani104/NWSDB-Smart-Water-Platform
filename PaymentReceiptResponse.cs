namespace NWSDB.API.DTOs;

public class PaymentReceiptResponse
{
    public int PaymentId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public decimal AmountPaid { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string BillStatus { get; set; } = string.Empty;
}