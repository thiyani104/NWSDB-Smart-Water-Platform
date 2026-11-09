using System.ComponentModel.DataAnnotations;

namespace NWSDB.API.DTOs;

/// <summary>
/// Registration payload. Deliberately has no Role property: the role is decided by the
/// server (Customer for public sign-up, Staff via the admin endpoint), never by the client.
/// </summary>
public class RegisterRequest
{
    [Required, StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(20, MinimumLength = 7)]
    public string Phone { get; set; } = string.Empty;
}
