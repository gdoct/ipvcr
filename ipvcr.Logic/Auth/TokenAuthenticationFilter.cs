using ipvcr.Logic.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace ipvcr.Logic.Auth;
public class TokenAuthenticationFilter : IAsyncAuthorizationFilter
{
    private readonly ITokenManager _tokenManager;

    public TokenAuthenticationFilter(ITokenManager tokenManager)
    {
        _tokenManager = tokenManager;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var authHeader = context.HttpContext.Request.Headers["Authorization"].ToString();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var tokenString = authHeader.Replace("Bearer ", "");
        var principal = _tokenManager.ValidateToken(tokenString);

        if (principal == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Set the user identity on the HttpContext
        context.HttpContext.User = principal;

        await Task.CompletedTask;
    }
}

public static class TokenAuthenticationMiddleWare
{
    public static void UseTokenAuthentication(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var tokenManager = context.RequestServices.GetService(typeof(ITokenManager)) as ITokenManager;
            ArgumentNullException.ThrowIfNull(tokenManager, nameof(tokenManager));
            var filter = new TokenAuthenticationFilter(tokenManager);
            var actionContext = new ActionContext
            {
                HttpContext = context,
                RouteData = context.GetRouteData(),
                ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor()
            };
            await filter.OnAuthorizationAsync(new AuthorizationFilterContext(actionContext, []));
            await next();
        });
    }
}