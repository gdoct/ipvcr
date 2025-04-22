using ipvcr.Logic.Api;
using ipvcr.Logic.Auth;
using ipvcr.Logic.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ipvcr.Web.Controllers.Api;

[Authorize]
[Route("api/settings")]
[ApiController]
[Produces("application/json")]
[Consumes("application/json")]
public class SettingsApiController(
    ILogger<SettingsApiController> logger,
    ISettingsService settingsManager) : ControllerBase
{
    private readonly ILogger<SettingsApiController> _logger = logger;
    private readonly ISettingsService _settingsManager = settingsManager;

    // GET: api/settings
    [HttpGet]
    [Route("scheduler")]
    public ActionResult<SchedulerSettings> GetSchedulerSettings()
    {
        return Ok(_settingsManager.SchedulerSettings);
    }

    // PUT: api/settings
    [HttpPut]
    [Route("scheduler")]
    public IActionResult UpdateSchedulerSettings([FromBody] SchedulerSettings settings)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _settingsManager.SchedulerSettings = settings;
        return NoContent();
    }

    // GET: api/settings/playlist
    [HttpGet]
    [Route("playlist")]
    public ActionResult<PlaylistSettings> GetPlaylistSettings()
    {
        return Ok(_settingsManager.PlaylistSettings);
    }

    // PUT: api/settings/playlist
    [HttpPut]
    [Route("playlist")]
    public IActionResult UpdatePlaylistSettings([FromBody] PlaylistSettings settings)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _settingsManager.PlaylistSettings = settings;
        return NoContent();
    }

    // GET: api/settings/ssl
    [HttpGet]
    [Route("ssl")]
    public ActionResult<SslSettings> GetSslSettings()
    {
        return Ok(_settingsManager.SslSettings);
    }

    // PUT: api/settings/ssl
    [HttpPut]
    [Route("ssl")]
    public IActionResult UpdateSslSettings([FromBody] SslSettings settings)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _settingsManager.SslSettings = settings;
        return NoContent();
    }

    // GET: api/settings/ffmpeg
    [HttpGet]
    [Route("ffmpeg")]
    public ActionResult<FfmpegSettings> GetFfmpegSettings()
    {
        return Ok(_settingsManager.FfmpegSettings);
    }

    // PUT: api/settings/ffmpeg
    [HttpPut]
    [Route("ffmpeg")]
    public IActionResult UpdateFfmpegSettings([FromBody] FfmpegSettings settings)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _settingsManager.FfmpegSettings = settings;
        return NoContent();
    }

    // POST: api/settings/upload-m3u
    [HttpPost("upload-m3u")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadM3u(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Please upload a valid M3U file.");
        }

        var uploadPath = _settingsManager.PlaylistSettings.M3uPlaylistPath;
        if (string.IsNullOrEmpty(uploadPath))
        {
            return BadRequest("Upload path is not configured.");
        }
        if (!Directory.Exists(uploadPath))
        {
            return BadRequest("Upload path does not exist.");
        }
        if (!Directory.Exists(uploadPath) || !new DirectoryInfo(uploadPath).Attributes.HasFlag(FileAttributes.Directory))
        {
            return BadRequest("Upload path is not a directory.");
        }
        if (!uploadPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
        {
            uploadPath += Path.DirectorySeparatorChar;
        }
        // test if we can write to the upload folder
        var testFilePath = Path.Combine(uploadPath, $"{Guid.NewGuid()}.bin");
        try
        {
            using (var stream = new FileStream(testFilePath, FileMode.Create))
            {
                await stream.WriteAsync([], 0, 0);
            }
            System.IO.File.Delete(testFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload path is not writable.");
            return BadRequest("Upload path is not writable.");
        }

        var filePath = Path.Combine(uploadPath, file.FileName);

        try
        {
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var settings = _settingsManager.PlaylistSettings;
            settings.M3uPlaylistPath = filePath;
            _settingsManager.PlaylistSettings = settings;

            _logger.LogDebug("M3U file uploaded successfully to {filePath}", filePath);

            return Ok(new { message = "M3U file uploaded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading M3U file.");
            return StatusCode(500, "An error occurred while uploading the file.");
        }
    }

    // POST: api/settings/ssl/certificate
    [HttpPost]
    [Route("ssl/certificate")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadSslCertificate(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Please upload a valid certificate file.");
        }

        // Get the SSL settings directory
        var settings = _settingsManager.SslSettings;
        var certificateDir = Path.GetDirectoryName(settings.CertificatePath);

        if (string.IsNullOrEmpty(certificateDir))
        {
            certificateDir = "/data/ssl-certificates";
        }

        // Ensure directory exists
        if (!Directory.Exists(certificateDir))
        {
            try
            {
                Directory.CreateDirectory(certificateDir);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create certificate directory at {certificateDir}", certificateDir);
                return BadRequest($"Failed to create certificate directory: {ex.Message}");
            }
        }

        // Generate a filename that includes timestamp to avoid conflicts
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (fileExtension != ".pfx" && fileExtension != ".p12")
        {
            return BadRequest("Only .pfx or .p12 certificate files are accepted.");
        }

        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var fileName = $"certificate_{timestamp}{fileExtension}";
        var filePath = Path.Combine(certificateDir, fileName);

        try
        {
            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Update the settings
            settings.CertificatePath = filePath;
            _settingsManager.SslSettings = settings;

            _logger.LogInformation("SSL certificate uploaded successfully to {filePath}", filePath);

            return Ok(new { message = "Certificate uploaded successfully", path = filePath });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading SSL certificate.");
            return StatusCode(500, $"An error occurred while uploading the certificate: {ex.Message}");
        }
    }

    [HttpPost]
    [Route("ssl/generate-certificate")]
    public IActionResult GenerateSelfSignedCertificate()
    {
        // Get the SSL settings
        var settings = _settingsManager.SslSettings;
        if (!settings.UseSsl)
        {
            return BadRequest("SSL is not enabled in settings.");
        }

        var certificateDir = Path.GetDirectoryName(settings.CertificatePath);

        if (string.IsNullOrEmpty(certificateDir))
        {
            certificateDir = "/data/ssl-certificates";
        }

        // Ensure directory exists
        if (!Directory.Exists(certificateDir))
        {
            try
            {
                Directory.CreateDirectory(certificateDir);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create certificate directory at {certificateDir}", certificateDir);
                return BadRequest($"Failed to create certificate directory: {ex.Message}");
            }
        }

        try
        {
            // Generate a timestamped filename
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var filePath = Path.Combine(certificateDir, $"selfsigned_certificate_{timestamp}.pfx");

            // Create the certificate generator
            var certificateGenerator = new SelfSignedCertificateGenerator(new System.IO.Abstractions.FileSystem());

            // Generate the certificate 
            var certificate = certificateGenerator.GenerateSelfSignedTlsCertificate(filePath, settings.CertificatePassword);

            // Update the settings
            settings.CertificatePath = filePath;
            _settingsManager.SslSettings = settings;

            _logger.LogInformation("Self-signed certificate generated successfully at {filePath}", filePath);

            return Ok(new { message = "Self-signed certificate generated successfully", path = filePath });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating self-signed certificate");
            return StatusCode(500, $"An error occurred while generating the certificate: {ex.Message}");
        }
    }

    [HttpGet]
    [Route("adminsettings")]
    public IActionResult GetAdminSettings()
    {
        return Ok(_settingsManager.AdminPasswordSettings);
    }

    [HttpPut]
    [Route("adminsettings")]
    public IActionResult UpdateAdminSettings([FromBody] AdminPasswordSettings adminPasswordSettings)
    {
        if (adminPasswordSettings == null)
        {
            return BadRequest("Admin password settings cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(adminPasswordSettings.AdminUsername))
        {
            return BadRequest("Admin username cannot be empty.");
        }

        _settingsManager.AdminPasswordSettings = adminPasswordSettings;
        return NoContent();
    }
}