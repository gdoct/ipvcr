namespace ipvcr.Web.Controllers.Api;
using Microsoft.AspNetCore.Mvc;
using ipvcr.Web.Models;
using Microsoft.AspNetCore.Authorization;
using ipvcr.Logic.Api;

[Route("api/login")]
[ApiController]
[Produces("application/json")]
[Consumes("application/json")]
public class LoginController : ControllerBase
{
    private readonly ITokenManager _tokenManager;
    private readonly ISettingsService _settingsService;

    public LoginController(ITokenManager tokenManager, ISettingsService settingsService)
    {
        _tokenManager = tokenManager;
        _settingsService = settingsService;
    }

    [HttpPost]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (request.Username == _settingsService.AdminPasswordSettings.AdminUsername
         && _settingsService.ValidateAdminPassword(request.Password))
        {
            var token = _tokenManager.CreateToken(request.Username);
            return Ok(new { Token = token });
        }
        return Unauthorized();
    }

    [Authorize]
    [HttpPost]
    [Route("changepassword")]
    public IActionResult UpdatePassword([FromBody] LoginRequest request)
    {
        if (request.Username == _settingsService.AdminPasswordSettings.AdminUsername)
        {
            _settingsService.UpdateAdminPassword(request.Password);
            return Ok(new { Message = "Password updated successfully." });
        }
        return Unauthorized();
    }

    [Authorize]
    [HttpPost]
    [Route("restart")]
    public IActionResult Restart()
    {
        // restart the asp.net server
        Console.WriteLine("Server is restarting..."); 
        Program.ExitAspNetProcess(); 

        return Ok(new { Message = "Restarting..." });
    }

    [Authorize]
    [HttpPost]
    [Route("resetdefaults")]
    public IActionResult ResetDefaults()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            return Unauthorized();
        }
        if (_settingsService.AdminPasswordSettings.AdminUsername == username)
        {
            // reset the settings to defaults
            _settingsService.ResetFactoryDefaults();
            Program.ExitAspNetProcess(); // Restart the server after resetting settings
            return Ok(new { Message = "Settings reset to defaults." });
        }
        return Unauthorized();
    }
}