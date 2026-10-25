namespace NWSDB.API.Models;

public class ActivityLog
{
    public int Id { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}