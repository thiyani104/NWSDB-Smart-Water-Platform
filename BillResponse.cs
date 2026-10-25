namespace NWSDB.API.DTOs;

public class BillResponse
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public DateTime BillingDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}