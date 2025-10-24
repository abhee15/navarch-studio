using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Shared.Services;

public class CognitoJwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CognitoJwtService> _logger;
    private readonly IMemoryCache _cache;
    private readonly HttpClient _httpClient;
    private readonly string _userPoolId;
    private readonly string _region;
    private readonly string _issuer;

    public CognitoJwtService(
        IConfiguration configuration,
        ILogger<CognitoJwtService> logger,
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _cache = cache;
        _httpClient = httpClientFactory.CreateClient();
        _userPoolId = _configuration["Cognito:UserPoolId"]
            ?? throw new InvalidOperationException("Cognito:UserPoolId not configured");
        _region = _configuration["Cognito:Region"]
            ?? throw new InvalidOperationException("Cognito:Region not configured");
        _issuer = $"https://cognito-idp.{_region}.amazonaws.com/{_userPoolId}";
    }

    public async Task<ClaimsPrincipal?> ValidateTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Get public keys from Cognito
            var keys = await GetCognitoPublicKeysAsync(cancellationToken);
            var key = keys.FirstOrDefault(k => k.KeyId == jwtToken.Header.Kid);

            if (key == null)
            {
                _logger.LogWarning("No matching key found for kid: {Kid}", jwtToken.Header.Kid);
                return null;
            }

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _configuration["Cognito:ClientId"],
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var principal = handler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("JWT validation failed: {Error}", ex.Message);
            return null;
        }
    }

    public string? GetUserId(ClaimsPrincipal principal)
    {
        return principal.FindFirst("sub")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public string? GetUserEmail(ClaimsPrincipal principal)
    {
        return principal.FindFirst("email")?.Value
            ?? principal.FindFirst(ClaimTypes.Email)?.Value;
    }

    private async Task<List<RsaSecurityKey>> GetCognitoPublicKeysAsync(CancellationToken cancellationToken)
    {
        // Cache keys for 24 hours
        const string cacheKey = "CognitoPublicKeys";
        if (_cache.TryGetValue<List<RsaSecurityKey>>(cacheKey, out var cachedKeys) && cachedKeys != null)
        {
            return cachedKeys;
        }

        var jwksUrl = $"{_issuer}/.well-known/jwks.json";
        var response = await _httpClient.GetStringAsync(jwksUrl, cancellationToken);
        var jwks = JsonSerializer.Deserialize<JsonWebKeySet>(response);

        if (jwks?.Keys == null || jwks.Keys.Count == 0)
        {
            throw new InvalidOperationException("No keys found in JWKS");
        }

        var keys = new List<RsaSecurityKey>();
        foreach (var key in jwks.Keys)
        {
            if (key.Kty != "RSA" || string.IsNullOrEmpty(key.N) || string.IsNullOrEmpty(key.E))
            {
                continue;
            }

            var rsa = RSA.Create();
            rsa.ImportParameters(new RSAParameters
            {
                Modulus = Base64UrlDecode(key.N),
                Exponent = Base64UrlDecode(key.E)
            });

            var rsaKey = new RsaSecurityKey(rsa)
            {
                KeyId = key.Kid
            };
            keys.Add(rsaKey);
        }

        // Cache for 24 hours
        _cache.Set(cacheKey, keys, TimeSpan.FromHours(24));
        return keys;
    }

    private static byte[] Base64UrlDecode(string input)
    {
        input = input.Replace('-', '+').Replace('_', '/');
        switch (input.Length % 4)
        {
            case 2: input += "=="; break;
            case 3: input += "="; break;
        }
        return Convert.FromBase64String(input);
    }
}

// Helper classes for deserializing JWKS
public class JsonWebKeySet
{
    public List<JsonWebKey> Keys { get; set; } = new();
}

public class JsonWebKey
{
    public string Kty { get; set; } = "";
    public string Kid { get; set; } = "";
    public string Use { get; set; } = "";
    public string N { get; set; } = "";
    public string E { get; set; } = "";
}






