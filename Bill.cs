namespace NWSDB.API.Models;

public class Bill
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public DateTime BillingDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(30);
    public decimal Amount { get; set; }
    
    // Tracks unpaid balance for exact settlement & overpayment prevention
    public decimal OutstandingBalance { get; set; }

    public string Status { get; set; } = "Unpaid"; // Unpaid, Partially Paid, Paid
    
    public int WaterConnectionId { get; set; }
    public WaterConnection? WaterConnection { get; set; }
    
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}