using ipvcr.Logic.Api;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;

namespace ipvcr.Logic.Services;

/// <summary>
/// Implementation of the folder service for managing folders within the media directory
/// </summary>
public class FolderService : IFolderService
{
    private readonly ILogger<FolderService> _logger;
    private readonly ISettingsService _settingsService;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Constructor for the folder service
    /// </summary>
    public FolderService(
        ILogger<FolderService> logger,
        ISettingsService settingsService,
        IFileSystem fileSystem)
    {
        _logger = logger;
        _settingsService = settingsService;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public IEnumerable<FolderItem> ListFolders(string relativePath)
    {
        _logger.LogInformation("Listing folders for path: {RelativePath}", relativePath);

        // Make sure MediaPath exists
        string baseMediaPath = GetAndValidateMediaPath();

        // Sanitize and normalize the requested path to prevent directory traversal
        string fullPath = GetFullPath(relativePath);

        // Validate the resulting path is within the media directory
        if (!IsPathWithinMediaPath(fullPath))
        {
            _logger.LogWarning("Attempted access to path outside media directory: {RelativePath}", relativePath);
            throw new UnauthorizedAccessException("Access denied. Cannot browse outside the media folder.");
        }

        // Check if directory exists
        if (!_fileSystem.Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {relativePath}");
        }

        var result = new List<FolderItem>();

        // Add parent directory entry if not in root media path
        if (!string.Equals(fullPath, baseMediaPath, StringComparison.OrdinalIgnoreCase))
        {
            var parentPath = Path.GetDirectoryName(fullPath);
            // Make sure parent path is not outside the media path
            if (parentPath != null && IsPathWithinMediaPath(parentPath))
            {
                result.Add(new FolderItem
                {
                    Name = "..",
                    IsDirectory = true,
                    RelativePath = GetRelativePath(parentPath)
                });
            }
            else
            {
                // If parent would be outside media path, add root media path as parent
                result.Add(new FolderItem
                {
                    Name = "..",
                    IsDirectory = true,
                    RelativePath = ""  // Empty path means root media folder
                });
            }
        }

        // Get directories
        var directories = _fileSystem.Directory.GetDirectories(fullPath)
            .Where(IsPathWithinMediaPath) // Extra safety check
            .Select(d => new FolderItem
            {
                Name = Path.GetFileName(d),
                IsDirectory = true,
                RelativePath = GetRelativePath(d)
            })
            .OrderBy(d => d.Name);

        result.AddRange(directories);

        return result;
    }

    /// <inheritdoc />
    public FolderItem CreateFolder(string parentPath, string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
        {
            throw new ArgumentException("Folder name is required", nameof(folderName));
        }

        string cleanParentPath = string.IsNullOrEmpty(parentPath) ? "" : parentPath;

        _logger.LogInformation("Creating folder {FolderName} in {ParentPath}", folderName, cleanParentPath);

        // Make sure MediaPath exists
        GetAndValidateMediaPath();

        // Sanitize folder name to prevent directory traversal attacks
        var cleanFolderName = folderName.Trim();
        cleanFolderName = Path.GetFileName(cleanFolderName); // Strip any path components

        if (string.IsNullOrWhiteSpace(cleanFolderName) || cleanFolderName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Invalid folder name", nameof(folderName));
        }

        // Get full path of parent directory
        string parentFullPath = GetFullPath(cleanParentPath);

        // Ensure parent path is within media directory
        if (!IsPathWithinMediaPath(parentFullPath))
        {
            _logger.LogWarning("Attempted to create folder outside media directory: {ParentPath}", cleanParentPath);
            throw new UnauthorizedAccessException("Access denied. Cannot create folders outside the media folder.");
        }

        // Create parent directory if it doesn't exist
        if (!_fileSystem.Directory.Exists(parentFullPath))
        {
            _fileSystem.Directory.CreateDirectory(parentFullPath);
        }

        // Create the full path for the new folder
        var newFolderFullPath = Path.Combine(parentFullPath, cleanFolderName);

        // Final check to ensure the new path is valid
        if (!IsPathWithinMediaPath(newFolderFullPath))
        {
            throw new UnauthorizedAccessException("Invalid folder name or path");
        }

        // Check if folder already exists
        if (_fileSystem.Directory.Exists(newFolderFullPath))
        {
            throw new IOException($"Folder already exists: {cleanFolderName}");
        }

        // Create the directory
        _fileSystem.Directory.CreateDirectory(newFolderFullPath);

        // Return the new folder info with relative path
        return new FolderItem
        {
            Name = cleanFolderName,
            IsDirectory = true,
            RelativePath = GetRelativePath(newFolderFullPath)
        };
    }

    /// <inheritdoc />
    public bool FolderExists(string relativePath)
    {
        try
        {
            string fullPath = GetFullPath(relativePath);

            // Validate the path is within the media directory
            if (!IsPathWithinMediaPath(fullPath))
            {
                return false;
            }

            return _fileSystem.Directory.Exists(fullPath);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets and validates the media path from settings
    /// </summary>
    /// <returns>The validated media path</returns>
    private string GetAndValidateMediaPath()
    {
        string baseMediaPath = _settingsService.SchedulerSettings.MediaPath;
        if (string.IsNullOrEmpty(baseMediaPath))
        {
            throw new InvalidOperationException("Media path is not configured");
        }

        if (!_fileSystem.Directory.Exists(baseMediaPath))
        {
            throw new DirectoryNotFoundException($"Configured media path does not exist: {baseMediaPath}");
        }

        return baseMediaPath;
    }

    /// <summary>
    /// Helper method to validate path is within media directory
    /// </summary>
    /// <param name="path">The path to validate</param>
    /// <returns>True if the path is within the media directory, false otherwise</returns>
    private bool IsPathWithinMediaPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        // Get canonical paths to prevent directory traversal with ../ etc.
        try
        {
            string baseMediaPath = Path.GetFullPath(_settingsService.SchedulerSettings.MediaPath);
            string fullPath = Path.GetFullPath(path);

            // Make sure the path starts with the base media path
            return fullPath.StartsWith(baseMediaPath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // Any exception while normalizing paths is a sign of invalid path
            return false;
        }
    }

    /// <summary>
    /// Helper to convert relative path to full path
    /// </summary>
    /// <param name="relativePath">The relative path</param>
    /// <returns>The full path</returns>
    private string GetFullPath(string relativePath)
    {
        string baseMediaPath = _settingsService.SchedulerSettings.MediaPath;

        // If path is empty, return the base media path
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return baseMediaPath;
        }

        if (relativePath.StartsWith(baseMediaPath))
        {
            return relativePath; // Already a full path
        }

        // Normalize path separators
        relativePath = relativePath.Replace('\\', Path.DirectorySeparatorChar)
                                  .Replace('/', Path.DirectorySeparatorChar);

        // Remove any leading path separator
        if (relativePath.StartsWith(Path.DirectorySeparatorChar.ToString()))
        {
            relativePath = relativePath.Substring(1);
        }

        // Combine with base path
        string fullPath = Path.Combine(baseMediaPath, relativePath);

        // Get canonical path to prevent directory traversal with ../ etc.
        return Path.GetFullPath(fullPath);
    }

    /// <summary>
    /// Helper to convert full path to relative path
    /// </summary>
    /// <param name="fullPath">The full path</param>
    /// <returns>The relative path</returns>
    private string GetRelativePath(string fullPath)
    {
        string baseMediaPath = _settingsService.SchedulerSettings.MediaPath;

        if (string.Equals(fullPath, baseMediaPath, StringComparison.OrdinalIgnoreCase))
        {
            return ""; // Root folder
        }

        if (fullPath.StartsWith(baseMediaPath, StringComparison.OrdinalIgnoreCase))
        {
            string relativePath = fullPath.Substring(baseMediaPath.Length);
            // Remove any leading path separator
            if (relativePath.StartsWith(Path.DirectorySeparatorChar.ToString()))
            {
                relativePath = relativePath.Substring(1);
            }
            return relativePath;
        }

        // Should not happen if IsPathWithinMediaPath was called first
        throw new InvalidOperationException("Path is outside the media directory");
    }
}