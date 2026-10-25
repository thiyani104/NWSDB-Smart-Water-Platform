namespace NWSDB.API.Models;

public class MeterReading
{
    public int Id { get; set; }
    public DateTime ReadingDate { get; set; } = DateTime.UtcNow;
    public double ReadingValue { get; set; }
    
    // Foreign Key linking to WaterConnection
    public int WaterConnectionId { get; set; }
    public WaterConnection? WaterConnection { get; set; }
    
    // Foreign Key linking to Staff member who recorded it
    public int RecordedByStaffId { get; set; }
    public User? RecordedByStaff { get; set; }
}