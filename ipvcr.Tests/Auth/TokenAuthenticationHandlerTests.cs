// filepath: /home/guido/dotnet/ipvcr/ipvcr.Tests/TokenAuthenticationHandlerTests.cs
using ipvcr.Logic.Api;
using ipvcr.Logic.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace ipvcr.Tests.Auth;

#pragma warning disable CS0618 // Type or member is obsolete - suppressing warnings for ISystemClock
public class TokenAuthenticationHandlerTests
{
    private readonly Mock<ITokenManager> _mockTokenManager;
    private readonly Mock<IOptionsMonitor<AuthenticationSchemeOptions>> _mockOptions;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<UrlEncoder> _mockEncoder;
    private readonly Mock<ISystemClock> _mockClock;
    private readonly Mock<ILogger<TokenAuthenticationHandler>> _mockLogger;
    private readonly AuthenticationSchemeOptions _authOptions;
    private readonly HttpContext _httpContext;

    public TokenAuthenticationHandlerTests()
    {
        _mockTokenManager = new Mock<ITokenManager>();
        _mockOptions = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockEncoder = new Mock<UrlEncoder>();
        _mockClock = new Mock<ISystemClock>();
        _mockLogger = new Mock<ILogger<TokenAuthenticationHandler>>();
        _authOptions = new AuthenticationSchemeOptions();

        _mockOptions.Setup(x => x.Get(It.IsAny<string>())).Returns(_authOptions);
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);

        _httpContext = new DefaultHttpContext();
    }

    private TokenAuthenticationHandler CreateHandler()
    {
        var handler = new TokenAuthenticationHandler(
            _mockOptions.Object,
            _mockLoggerFactory.Object,
            _mockEncoder.Object,
            _mockClock.Object,
            _mockTokenManager.Object);

        handler.InitializeAsync(new AuthenticationScheme("Bearer", "Bearer", typeof(TokenAuthenticationHandler)), _httpContext).Wait();

        return handler;
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WithValidToken_ReturnsSuccess()
    {
        // Arrange
        var tokenString = "valid-token";

        // Fix ASP0019: Use indexer instead of Add
        _httpContext.Request.Headers["Authorization"] = $"Bearer {tokenString}";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "testuser"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        _mockTokenManager.Setup(x => x.ValidateToken(tokenString)).Returns(principal);

        var handler = CreateHandler();

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.Same(principal, result.Principal);
        Assert.Equal("Bearer", result.Ticket?.AuthenticationScheme);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WithMissingAuthHeader_ReturnsNoResult()
    {
        // Arrange
        // No Authorization header set
        var handler = CreateHandler();

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.Failure);  // No failure for NoResult
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WithEmptyAuthHeader_ReturnsNoResult()
    {
        // Arrange
        // Fix ASP0019: Use indexer instead of Add
        _httpContext.Request.Headers["Authorization"] = string.Empty;

        var handler = CreateHandler();

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.Failure);  // No failure for NoResult
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WithNonBearerAuth_ReturnsNoResult()
    {
        // Arrange
        // Fix ASP0019: Use indexer instead of Add
        _httpContext.Request.Headers["Authorization"] = "Basic dXNlcm5hbWU6cGFzc3dvcmQ=";

        var handler = CreateHandler();

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.Null(result.Failure);  // No failure for NoResult
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WithInvalidToken_ReturnsFailure()
    {
        // Arrange
        var tokenString = "invalid-token";

        // Fix ASP0019: Use indexer instead of Add
        _httpContext.Request.Headers["Authorization"] = $"Bearer {tokenString}";

        // Suppress CS8600: Converting null literal to non-nullable type
        ClaimsPrincipal? p = null;
        _mockTokenManager.Setup(x => x.ValidateToken(tokenString)).Returns(p);

        var handler = CreateHandler();

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
        Assert.Contains("Invalid token", result.Failure.Message);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WithTokenWithRoles_ReturnsPrincipalWithRoles()
    {
        // Arrange
        var tokenString = "valid-token-with-roles";

        // Fix ASP0019: Use indexer instead of Add
        _httpContext.Request.Headers["Authorization"] = $"Bearer {tokenString}";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "testuser"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "User")
        };

        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        _mockTokenManager.Setup(x => x.ValidateToken(tokenString)).Returns(principal);

        var handler = CreateHandler();

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.Same(principal, result.Principal);

        // Suppress CS8604: Possible null reference argument
        //#pragma warning disable CS8604
        var newprincipal = result.Principal;
        Assert.NotNull(newprincipal);
        Assert.Equal(2, newprincipal.FindAll(ClaimTypes.Role).Count());
        Assert.Contains(newprincipal.FindAll(ClaimTypes.Role),
            c => c.Value == "Admin");
        Assert.Contains(newprincipal.FindAll(ClaimTypes.Role),
            c => c.Value == "User");
        //#pragma warning restore CS8604
    }
}
#pragma warning restore CS0618 // Type or member is obsolete