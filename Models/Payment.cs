namespace NWSDB.API.Models;

public class Payment
{
    public int Id { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Online"; // Online, Gateway, PartnerAPI
    public string PaymentStatus { get; set; } = "Success"; // Success, Failed
    
    public int BillId { get; set; }
    public Bill? Bill { get; set; }
    
    // Composition relation: A payment has one Receipt
    public PaymentReceipt? Receipt { get; set; }
}