using System.Security.Claims;

namespace ipvcr.Logic.Api;

public interface ITokenManager
{
    string CreateToken(string username, IEnumerable<string>? roles = null);
    ClaimsPrincipal? ValidateToken(string token);
    // No need for InvalidateToken methods as we'll use JWT expiration instead

    string CreateHash(string input);
}
