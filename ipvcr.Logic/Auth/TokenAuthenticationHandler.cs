using ipvcr.Logic.Api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace ipvcr.Logic.Auth;

#pragma warning disable CS0618 // Type or member ISystemClock is obsolete
public class TokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ITokenManager _tokenManager;

    public TokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        ITokenManager tokenManager)
        : base(options, logger, encoder, clock)
    {
        _tokenManager = tokenManager;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Get authorization header
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader) ||
            string.IsNullOrEmpty(authHeader) ||
            !authHeader.ToString().StartsWith("Bearer "))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var tokenString = authHeader.ToString().Replace("Bearer ", "");
        var principal = _tokenManager.ValidateToken(tokenString);

        if (principal == null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid token"));
        }

        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
