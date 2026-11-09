namespace NWSDB.API.Models;

public class SystemConfiguration
{
    public int Id { get; set; }
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public string ModifiedBy { get; set; } = "System";
}