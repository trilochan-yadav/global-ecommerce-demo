using BFF.API.Business.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BFF.API.Business.Implementations;

public class AuthService : IAuthService
{
    private readonly IConfiguration _config;

    public AuthService(IConfiguration config) => _config = config;

    public string? GenerateToken(string username, string password)
    {
        if (password != "pass") return null;

        string role;
        if (username.Contains("admin", StringComparison.OrdinalIgnoreCase))
            role = "Admin";
        else if (username.StartsWith("user", StringComparison.OrdinalIgnoreCase))
            role = "Customer";
        else
            return null;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:ExpiryMinutes"]!));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),   // used by [Authorize(Roles="Admin")]
                new Claim("role", role)              // used by Angular JWT decode
            },
            expires: expiry,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
