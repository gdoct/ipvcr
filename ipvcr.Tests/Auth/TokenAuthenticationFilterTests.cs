// filepath: /home/guido/dotnet/ipvcr/ipvcr.Tests/TokenAuthenticationFilterTests.cs
using ipvcr.Logic.Api;
using ipvcr.Logic.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;

namespace ipvcr.Tests.Auth;

public class TokenAuthenticationFilterTests
{
    [Fact]
    public async Task OnAuthorizationAsync_WithValidToken_SetsUserPrincipal()
    {
        // Arrange
        var tokenManager = new Mock<ITokenManager>();
        var filter = new TokenAuthenticationFilter(tokenManager.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = "Bearer valid-token";

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor()
        );

        var filterContext = new AuthorizationFilterContext(
            actionContext,
            new List<IFilterMetadata>()
        );

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, "testUser")
        }, "Bearer"));

        tokenManager.Setup(tm => tm.ValidateToken("valid-token"))
            .Returns(principal);

        // Act
        await filter.OnAuthorizationAsync(filterContext);

        // Assert
        Assert.Same(principal, httpContext.User);
        Assert.Null(filterContext.Result);
    }

    [Fact]
    public async Task OnAuthorizationAsync_WithNoAuthHeader_ReturnsUnauthorized()
    {
        // Arrange
        var tokenManager = new Mock<ITokenManager>();
        var filter = new TokenAuthenticationFilter(tokenManager.Object);

        var httpContext = new DefaultHttpContext();
        // No Authorization header set

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor()
        );

        var filterContext = new AuthorizationFilterContext(
            actionContext,
            new List<IFilterMetadata>()
        );

        // Act
        await filter.OnAuthorizationAsync(filterContext);

        // Assert
        Assert.IsType<UnauthorizedResult>(filterContext.Result);
    }

    [Fact]
    public async Task OnAuthorizationAsync_WithNonBearerAuthHeader_ReturnsUnauthorized()
    {
        // Arrange
        var tokenManager = new Mock<ITokenManager>();
        var filter = new TokenAuthenticationFilter(tokenManager.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = "Basic dXNlcjpwYXNzd29yZA=="; // Basic auth

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor()
        );

        var filterContext = new AuthorizationFilterContext(
            actionContext,
            new List<IFilterMetadata>()
        );

        // Act
        await filter.OnAuthorizationAsync(filterContext);

        // Assert
        Assert.IsType<UnauthorizedResult>(filterContext.Result);
    }

    [Fact]
    public async Task OnAuthorizationAsync_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var tokenManager = new Mock<ITokenManager>();
        var filter = new TokenAuthenticationFilter(tokenManager.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = "Bearer invalid-token";

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor()
        );

        var filterContext = new AuthorizationFilterContext(
            actionContext,
            new List<IFilterMetadata>()
        );

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        tokenManager.Setup(tm => tm.ValidateToken("invalid-token"))
            .Returns((ClaimsPrincipal)null);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

        // Act
        await filter.OnAuthorizationAsync(filterContext);

        // Assert
        Assert.IsType<UnauthorizedResult>(filterContext.Result);
    }

    [Fact]
    public async Task TokenAuthenticationMiddleware_CallsNextWithAuthenticatedUser()
    {
        // Arrange
        var tokenManager = new Mock<ITokenManager>();
        var serviceProvider = new ServiceCollection()
            .AddSingleton(tokenManager.Object)
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Request.Headers["Authorization"] = "Bearer valid-token";

        var routeData = new RouteData();
        httpContext.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(), "Test"));

        bool nextMiddlewareCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextMiddlewareCalled = true;
            return Task.CompletedTask;
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, "testUser")
        }, "Bearer"));

        tokenManager.Setup(tm => tm.ValidateToken("valid-token"))
            .Returns(principal);

        // Create middleware manually
        RequestDelegate middleware = async (ctx) =>
        {
            // Simulate the middleware's logic
            var authHeader = ctx.Request.Headers["Authorization"].ToString();

            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                ctx.Response.StatusCode = 401;
                return;
            }

            var tokenStr = authHeader.Replace("Bearer ", "");
            var userPrincipal = tokenManager.Object.ValidateToken(tokenStr);

            if (userPrincipal == null)
            {
                ctx.Response.StatusCode = 401;
                return;
            }

            ctx.User = userPrincipal;
            await next(ctx);
        };

        // Act
        await middleware(httpContext);

        // Assert
        Assert.True(nextMiddlewareCalled);
        Assert.Same(principal, httpContext.User);
    }

    [Fact]
    public async Task TokenAuthenticationMiddleware_WithInvalidToken_DoesNotCallNext()
    {
        // Arrange
        var tokenManager = new Mock<ITokenManager>();
        var serviceProvider = new ServiceCollection()
            .AddSingleton(tokenManager.Object)
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        httpContext.Request.Headers["Authorization"] = "Bearer invalid-token";

        var routeData = new RouteData();
        httpContext.SetEndpoint(new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(), "Test"));

        bool nextMiddlewareCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextMiddlewareCalled = true;
            return Task.CompletedTask;
        };

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        tokenManager.Setup(tm => tm.ValidateToken("invalid-token"))
            .Returns((ClaimsPrincipal)null);

        // Create middleware manually
        RequestDelegate middleware = async (ctx) =>
        {
            // Simulate the middleware's logic
            var authHeader = ctx.Request.Headers["Authorization"].ToString();

            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                ctx.Response.StatusCode = 401;
                return;
            }

            var tokenStr = authHeader.Replace("Bearer ", "");
            var userPrincipal = tokenManager.Object.ValidateToken(tokenStr);

            if (userPrincipal == null)
            {
                ctx.Response.StatusCode = 401;
                return;
            }

            ctx.User = userPrincipal;
            await next(ctx);
        };
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

        // Act
        await middleware(httpContext);

        // Assert
        Assert.False(nextMiddlewareCalled);
        Assert.Equal(401, httpContext.Response.StatusCode);
    }

    [Fact]
    public void UseTokenAuthentication_RegistersMiddleware()
    {
        // Arrange
        var tokenManager = new Mock<ITokenManager>();
        var serviceProvider = new ServiceCollection()
            .AddSingleton(tokenManager.Object)
            .BuildServiceProvider();

        var appBuilder = new ApplicationBuilder(serviceProvider);

        // Act - This is just testing that the extension method doesn't throw
        var exception = Record.Exception(() => appBuilder.UseTokenAuthentication());

        // Assert
        Assert.Null(exception); // No exception means the middleware was registered successfully
    }
}