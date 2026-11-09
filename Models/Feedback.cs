namespace NWSDB.API.Models;

public class Feedback
{
    public int Id { get; set; }
    
    // Links feedback directly to the authenticated user submitting it
    public int UserId { get; set; }
    public User? User { get; set; }

    public string AccountNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending"; // Pending, Reviewed
}