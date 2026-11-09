namespace NWSDB.API.Models;

public class WaterConnection
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string ConnectionAddress { get; set; } = string.Empty;
    public string ConnectionStatus { get; set; } = "Active"; // Active, Disconnected, Pending
    
    // Foreign Key linking back to User (Customer)
    public int UserId { get; set; }
    public User? User { get; set; }
    
    public ICollection<MeterReading> MeterReadings { get; set; } = new List<MeterReading>();
}