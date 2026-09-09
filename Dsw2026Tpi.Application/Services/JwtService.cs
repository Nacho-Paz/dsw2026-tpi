using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Dsw2026Tpi.Application.Services;

public class JwtService
{
    private readonly IConfiguration _config;
    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateToken(Guid userId, string username, string? role)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username is required.", nameof(username));
        if (string.IsNullOrWhiteSpace(role)) throw new ArgumentException("Role is required.", nameof(role));

        var jwtConfig = _config.GetSection("Jwt");

        var keyText = jwtConfig["Key"];
        if (string.IsNullOrWhiteSpace(keyText)) throw new InvalidOperationException("JWT Key is not configured.");

        var issuer = jwtConfig["Issuer"];
        if (string.IsNullOrWhiteSpace(issuer)) throw new InvalidOperationException("JWT Issuer is not configured.");

        var audience = jwtConfig["Audience"];
        if (string.IsNullOrWhiteSpace(audience)) throw new InvalidOperationException("JWT Audience is not configured.");
        
        var expiresIn = int.Parse(jwtConfig["ExpiresInMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyText));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role)
        };

        var expires = DateTime.UtcNow.AddMinutes(expiresIn);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
            );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return tokenString;
    }
}
