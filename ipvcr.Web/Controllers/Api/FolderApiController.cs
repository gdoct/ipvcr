using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using ipvcr.Logic.Api;

namespace ipvcr.Web.Controllers.Api;

[Authorize]
[Route("api/folders")]
[ApiController]
[Produces("application/json")]
public class FolderApiController : ControllerBase
{
    private readonly ILogger<FolderApiController> _logger;
    private readonly IFolderService _folderService;
    
    public FolderApiController(
        ILogger<FolderApiController> logger,
        IFolderService folderService)
    {
        _logger = logger;
        _folderService = folderService;
    }
    
    // GET api/folders/list?path=subfolder/nested
    [HttpGet("list")]
    public ActionResult<IEnumerable<FolderItemResponse>> GetFolderContents([FromQuery] string path = "")
    {
        _logger.LogInformation("Getting folder contents for path: {path}", path);
        
        try
        {
            var folders = _folderService.ListFolders(path);
            
            // Map to response model
            var response = folders.Select(f => new FolderItemResponse
            {
                Name = f.Name,
                IsDirectory = f.IsDirectory,
                FullPath = f.RelativePath
            });
            
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access to directory {path}", path);
            return StatusCode(403, "Unauthorized access to directory");
        }
        catch (System.IO.DirectoryNotFoundException ex)
        {
            _logger.LogError(ex, "Directory not found: {path}", path);
            return NotFound($"Directory not found: {path}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing directory contents for {path}", path);
            return StatusCode(500, "Error listing directory contents");
        }
    }
    
    // POST api/folders/create
    [HttpPost("create")]
    public ActionResult<FolderItemResponse> CreateFolder([FromBody] CreateFolderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FolderName))
        {
            return BadRequest("Folder name is required");
        }
        
        string parentPath = string.IsNullOrEmpty(request.ParentPath) ? "" : request.ParentPath;
        
        _logger.LogInformation("Creating folder {folderName} in {parentPath}", request.FolderName, parentPath);
        
        try
        {
            var createdFolder = _folderService.CreateFolder(parentPath, request.FolderName);
            
            // Map to response model
            var response = new FolderItemResponse
            {
                Name = createdFolder.Name,
                IsDirectory = createdFolder.IsDirectory,
                FullPath = createdFolder.RelativePath
            };
            
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid folder name: {folderName}", request.FolderName);
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access when creating directory {folderName} in {parentPath}", request.FolderName, parentPath);
            return StatusCode(403, "Unauthorized access when creating folder");
        }
        catch (System.IO.IOException ex) when (ex.Message.Contains("already exists"))
        {
            _logger.LogError(ex, "Folder already exists: {folderName} in {parentPath}", request.FolderName, parentPath);
            return Conflict($"Folder already exists: {request.FolderName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating directory {folderName} in {parentPath}", request.FolderName, parentPath);
            return StatusCode(500, $"Error creating directory: {ex.Message}");
        }
    }
    
    // Models
    public class FolderItemResponse
    {
        public string Name { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public string FullPath { get; set; } = string.Empty;
    }
    
    public class CreateFolderRequest
    {
        public string ParentPath { get; set; } = string.Empty;
        public string FolderName { get; set; } = string.Empty;
    }
}