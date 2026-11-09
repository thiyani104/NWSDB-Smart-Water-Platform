namespace NWSDB.API.DTOs;

public class MeterReadingRequest
{
    public string AccountNumber { get; set; } = string.Empty;
    public double ReadingValue { get; set; }
    public int RecordedByStaffId { get; set; }
}