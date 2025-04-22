namespace ipvcr.Logic.Api;

/// <summary>
/// Represents an item in a folder structure, could be a directory or file
/// </summary>
public class FolderItem
{
    /// <summary>
    /// Name of the folder or file
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether this item is a directory
    /// </summary>
    public bool IsDirectory { get; set; }

    /// <summary>
    /// Relative path from the media root folder
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;
}

/// <summary>
/// Service for managing folders within the media directory
/// </summary>
public interface IFolderService
{
    /// <summary>
    /// Lists folders at the specified relative path from the media directory
    /// </summary>
    /// <param name="relativePath">Relative path from the media directory; empty for root</param>
    /// <returns>Collection of folder items at the specified path</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when attempting to access a path outside the media directory</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory doesn't exist</exception>
    IEnumerable<FolderItem> ListFolders(string relativePath);

    /// <summary>
    /// Creates a new folder at the specified parent path
    /// </summary>
    /// <param name="parentPath">Parent path relative to media directory; empty for root</param>
    /// <param name="folderName">Name of the folder to create</param>
    /// <returns>The created folder item</returns>
    /// <exception cref="ArgumentException">Thrown when folder name is invalid</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when attempting to create a folder outside the media directory</exception>
    /// <exception cref="IOException">Thrown when the folder already exists</exception>
    FolderItem CreateFolder(string parentPath, string folderName);

    /// <summary>
    /// Checks if a folder exists at the specified relative path
    /// </summary>
    /// <param name="relativePath">Relative path from the media directory</param>
    /// <returns>True if the folder exists, false otherwise</returns>
    bool FolderExists(string relativePath);
}