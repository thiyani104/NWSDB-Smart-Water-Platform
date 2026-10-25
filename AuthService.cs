using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NWSDB.API.Data;
using NWSDB.API.DTOs;
using NWSDB.API.Models;

namespace NWSDB.API.Data.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<UserResponse?> RegisterAsync(RegisterRequest request, string role = "Customer")
    {
        // Prevent duplicate accounts by username or email
        var exists = await _context.Users.AnyAsync(u => 
            u.Username.ToLower() == request.Username.Trim().ToLower() || 
            u.Email.ToLower() == request.Email.Trim().ToLower());

        if (exists) return null;

        var user = new User
        {
            Username = request.Username.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            // The role is chosen by the server (caller), never by the client payload.
            Role = role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role,
            Token = token
        };
    }

    public async Task<UserResponse?> LoginAsync(LoginRequest request)
    {
        var identifier = request.Username.Trim().ToLower();

        // Allows login using either Username OR Email Address
        var user = await _context.Users.FirstOrDefaultAsync(u => 
            u.Username.ToLower() == identifier || 
            u.Email.ToLower() == identifier);

        if (user == null) return null;

        bool validPassword;
        try
        {
            validPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        }
        catch (Exception)
        {
            // Malformed stored hash: fail closed. Never fall back to a plain-text comparison.
            validPassword = false;
        }

        if (!validPassword) return null;

        var token = GenerateJwtToken(user);

        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role,
            Token = token
        };
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Secret Key is not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpiryInMinutes"] ?? "120")),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}