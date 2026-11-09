namespace NWSDB.API.DTOs;

public class MeterReadingResponse
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public double ReadingValue { get; set; }
    public DateTime ReadingDate { get; set; }
    public int RecordedByStaffId { get; set; }
    public decimal CalculatedBillAmount { get; set; }
}