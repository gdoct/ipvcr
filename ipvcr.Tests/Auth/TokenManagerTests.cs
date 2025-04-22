// filepath: /home/guido/dotnet/ipvcr/ipvcr.Tests/TokenManagerTests.cs
using ipvcr.Logic.Auth;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ipvcr.Tests.Auth;

public class TokenManagerTests
{
    private const string SecretKey = "unit_test_secret_key_at_least_32_chars";
    private const string Issuer = "TestIssuer";
    private const string Audience = "TestAudience";
    private const string Username = "testuser";

    [Fact]
    public void CreateToken_WithUsernameOnly_ReturnsValidToken()
    {
        // Arrange
        var tokenManager = new TokenManager(SecretKey, Issuer, Audience);

        // Act
        var token = tokenManager.CreateToken(Username);

        // Assert
        Assert.NotEmpty(token);
        var principal = ValidateToken(token);
        Assert.NotNull(principal);

        var usernameClaim = principal.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        Assert.NotNull(usernameClaim);
        Assert.Equal(Username, usernameClaim.Value);

        // Check that no role claims exist
        Assert.Empty(principal.FindAll(ClaimTypes.Role));
    }

    [Fact]
    public void CreateToken_WithRoles_ReturnsTokenWithRoles()
    {
        // Arrange
        var tokenManager = new TokenManager(SecretKey, Issuer, Audience);
        var roles = new[] { "Admin", "User" };

        // Act
        var token = tokenManager.CreateToken(Username, roles);

        // Assert
        Assert.NotEmpty(token);
        var principal = ValidateToken(token);
        Assert.NotNull(principal);

        // Check username claim
        var usernameClaim = principal.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        Assert.NotNull(usernameClaim);
        Assert.Equal(Username, usernameClaim.Value);

        // Check that role claims exist
        var roleClaims = principal.FindAll(ClaimTypes.Role).ToList();
        Assert.Equal(2, roleClaims.Count);
        Assert.Contains(roleClaims, c => c.Value == "Admin");
        Assert.Contains(roleClaims, c => c.Value == "User");
    }

    [Fact]
    public void ValidateToken_WithValidToken_ReturnsPrincipal()
    {
        // Arrange
        var tokenManager = new TokenManager(SecretKey, Issuer, Audience);
        var token = tokenManager.CreateToken(Username);

        // Act
        var principal = tokenManager.ValidateToken(token);

        // Assert
        Assert.NotNull(principal);
        var usernameClaim = principal.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        Assert.NotNull(usernameClaim);
        Assert.Equal(Username, usernameClaim.Value);
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var tokenManager = new TokenManager(SecretKey, Issuer, Audience);
        var invalidToken = "invalid.token.format";

        // Act
        var principal = tokenManager.ValidateToken(invalidToken);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_WithTokenFromDifferentIssuer_ReturnsNull()
    {
        // Arrange
        var tokenManager1 = new TokenManager(SecretKey, "Issuer1", Audience);
        var tokenManager2 = new TokenManager(SecretKey, "Issuer2", Audience);

        // Create token with Issuer1
        var token = tokenManager1.CreateToken(Username);

        // Validate token with tokenManager2 (different issuer)
        var principal = tokenManager2.ValidateToken(token);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_WithTokenFromDifferentAudience_ReturnsNull()
    {
        // Arrange
        var tokenManager1 = new TokenManager(SecretKey, Issuer, "Audience1");
        var tokenManager2 = new TokenManager(SecretKey, Issuer, "Audience2");

        // Create token with Audience1
        var token = tokenManager1.CreateToken(Username);

        // Validate token with tokenManager2 (different audience)
        var principal = tokenManager2.ValidateToken(token);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public async Task ValidateToken_WithExpiredToken_ReturnsNull()
    {
        // Arrange
        var tokenManager = new TokenManager(SecretKey, Issuer, Audience,
                                          tokenLifetimeMinutes: 0); // Token expires immediately

        // Create a token (which will expire immediately)
        var token = tokenManager.CreateToken(Username);

        // Add a small delay to ensure the token has expired
        await Task.Delay(1000).ContinueWith(_ =>
        {
            // Act
            var principal = tokenManager.ValidateToken(token);

            // Assert
            Assert.Null(principal);
        });
    }

    [Fact]
    public void CreateHash_WithSimpleString_ReturnsBase64EncodedString()
    {
        // Arrange
        var tokenManager = new TokenManager();
        var input = "testpassword";
        var expected = input.PadLeft(16, '0'); // Pad to 16 characters
        // Act
        var hash = tokenManager.CreateHash(input);

        // Assert
        Assert.NotEmpty(hash);

        // Verify we can decode it with Base64
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(hash));
        Assert.Equal(expected, decoded);
    }

    [Fact]
    public void CreateHash_WithEmptyString_ReturnsEmptyHash()
    {
        // Arrange
        var tokenManager = new TokenManager();

        // Act
        var hash = tokenManager.CreateHash(string.Empty);

        // Assert
        Assert.Empty(hash); // Even an empty string will produce a Base64 result
    }

    // Helper method to validate tokens manually for test assertions
    private static ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey)),
            ClockSkew = TimeSpan.Zero
        };

        return tokenHandler.ValidateToken(token, validationParameters, out _);
    }
}