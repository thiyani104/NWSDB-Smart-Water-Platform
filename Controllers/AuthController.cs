using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NWSDB.API.Data.Services;
using NWSDB.API.DTOs;

namespace NWSDB.API.Controllers;

[AllowAnonymous]
[EnableRateLimiting("auth")]
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Public customer registration endpoint.
    /// The role is always Customer; any role value in the payload is ignored (the DTO has no Role property).
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (request == null)
            return BadRequest(new { message = "Registration data cannot be empty." });

        try
        {
            var result = await _authService.RegisterAsync(request);
            if (result == null)
                return BadRequest(new { message = "Username or Email already exists." });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// User login endpoint returning an authenticated JWT Bearer token and user details.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (request == null)
            return BadRequest(new { message = "Login request cannot be empty." });

        var result = await _authService.LoginAsync(request);
        if (result == null)
            return Unauthorized(new { message = "Invalid username or password." });

        return Ok(result);
    }
}