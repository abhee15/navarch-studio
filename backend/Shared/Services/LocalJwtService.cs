using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Shared.Services;

/// <summary>
/// Local JWT service for development without AWS Cognito
/// Generates and validates JWT tokens using a local secret key
/// </summary>
public class LocalJwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LocalJwtService> _logger;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;

    public LocalJwtService(
        IConfiguration configuration,
        ILogger<LocalJwtService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Use configuration or default values for local development
        _secretKey = _configuration["Jwt:SecretKey"]
            ?? "navarch-studio-local-development-secret-key-min-32-chars";
        _issuer = _configuration["Jwt:Issuer"] ?? "navarch-studio-local";
        _audience = _configuration["Jwt:Audience"] ?? "navarch-studio-api";

        if (_secretKey.Length < 32)
        {
            throw new InvalidOperationException("JWT secret key must be at least 32 characters");
        }

        _logger.LogInformation("LocalJwtService initialized for local development");
    }

    public Task<ClaimsPrincipal?> ValidateTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return Task.FromResult<ClaimsPrincipal?>(principal);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JWT validation failed: {Error}", ex.Message);
            return Task.FromResult<ClaimsPrincipal?>(null);
        }
    }

    public string? GetUserId(ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;
    }

    public string? GetUserEmail(ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst("email")?.Value;
    }

    /// <summary>
    /// Generate a JWT token for a user (for local development only)
    /// </summary>
    public string GenerateToken(string userId, string email, string? name = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim("sub", userId),
            new Claim("email", email)
        };

        if (!string.IsNullOrEmpty(name))
        {
            claims.Add(new Claim(ClaimTypes.Name, name));
            claims.Add(new Claim("name", name));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(24),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

