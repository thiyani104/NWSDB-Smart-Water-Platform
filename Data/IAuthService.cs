using NWSDB.API.DTOs;

namespace NWSDB.API.Data.Services;

public interface IAuthService
{
    /// <summary>
    /// Creates a user. The role is a server-side decision and is never read from the request.
    /// </summary>
    Task<UserResponse?> RegisterAsync(RegisterRequest request, string role = "Customer");
    Task<UserResponse?> LoginAsync(LoginRequest request);
}
