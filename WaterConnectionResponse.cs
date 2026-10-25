namespace NWSDB.API.DTOs;

public class WaterConnectionResponse
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string ConnectionAddress { get; set; } = string.Empty;
    public string ConnectionStatus { get; set; } = string.Empty;
    public int UserId { get; set; }
}