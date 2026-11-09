namespace NWSDB.API.DTOs;

public class PaymentRequest
{
    public int BillId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Online"; // Online, Gateway, PartnerAPI
}