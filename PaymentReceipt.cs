namespace NWSDB.API.Models;

public class PaymentReceipt
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;
    
    public int PaymentId { get; set; }
    public Payment? Payment { get; set; }
}