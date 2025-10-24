using System.Security.Claims;

namespace Shared.Services;

public interface IJwtService
{
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
    string? GetUserId(ClaimsPrincipal principal);
    string? GetUserEmail(ClaimsPrincipal principal);
}






