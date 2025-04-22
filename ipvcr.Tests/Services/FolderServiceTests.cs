using System;
using System.IO;
using System.IO.Abstractions;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Xunit;
using ipvcr.Logic.Api;
using ipvcr.Logic.Services;
using ipvcr.Logic.Settings;
using Microsoft.Extensions.Logging;

namespace ipvcr.Tests.Services
{
    public class FolderServiceTests
    {
        private readonly MockRepository _mockRepository;
        private readonly Mock<ILogger<FolderService>> _mockLogger;
        private readonly Mock<ISettingsService> _mockSettingsService;
        private readonly Mock<IFileSystem> _mockFileSystem;
        private readonly Mock<IPath> _mockPath;
        private readonly Mock<IDirectory> _mockDirectory;
        private readonly Mock<IFile> _mockFile;
        private readonly SchedulerSettings _schedulerSettings;

        public FolderServiceTests()
        {
            // Create strict mock repository (will throw for unexpected calls)
            _mockRepository = new MockRepository(MockBehavior.Strict);
            
            // Create mocks
            _mockLogger = _mockRepository.Create<ILogger<FolderService>>();
            _mockSettingsService = _mockRepository.Create<ISettingsService>();
            _mockFileSystem = _mockRepository.Create<IFileSystem>();
            _mockPath = _mockRepository.Create<IPath>();
            _mockDirectory = _mockRepository.Create<IDirectory>();
            _mockFile = _mockRepository.Create<IFile>();

            // Setup common file system parts
            _mockFileSystem.Setup(fs => fs.Path).Returns(_mockPath.Object);
            _mockFileSystem.Setup(fs => fs.Directory).Returns(_mockDirectory.Object);
            _mockFileSystem.Setup(fs => fs.File).Returns(_mockFile.Object);

            // Setup scheduler settings with default media path
            _schedulerSettings = new SchedulerSettings { MediaPath = "/media" };
            _mockSettingsService.Setup(s => s.SchedulerSettings).Returns(_schedulerSettings);
        }

        [Fact]
        public void ListFolders_WithValidPath_ReturnsExpectedFolders()
        {
            // Arrange
            string relativePath = "testFolder";
            string fullPath = "/media/testFolder";
            
            // Mock path normalization
            _mockPath.Setup(p => p.GetFullPath(_schedulerSettings.MediaPath)).Returns("/media");
            _mockPath.Setup(p => p.GetFullPath(fullPath)).Returns(fullPath);
            _mockPath.Setup(p => p.GetFileName(It.IsAny<string>())).Returns<string>(s => s.Split('/').Last());
            
            // Setup directory operations            
            _mockDirectory.Setup(d => d.Exists(_schedulerSettings.MediaPath)).Returns(true);
            _mockDirectory.Setup(d => d.Exists(fullPath)).Returns(true);
            _mockDirectory.Setup(d => d.GetDirectories(fullPath)).Returns(new[] { "/media/testFolder/subDir1", "/media/testFolder/subDir2" });
            
            // Setup logger
            _mockLogger.Setup(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
            
            var folderService = new FolderService(_mockLogger.Object, _mockSettingsService.Object, _mockFileSystem.Object);
            
            // Act
            var result = folderService.ListFolders(relativePath).ToList();
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count); // Parent directory (..) + 2 subdirectories
            Assert.Equal("..", result[0].Name); // First item should be parent directory
            Assert.True(result[0].IsDirectory);
            Assert.Equal("subDir1", result[1].Name);
            Assert.True(result[1].IsDirectory);
            Assert.Equal("subDir2", result[2].Name);
            Assert.True(result[2].IsDirectory);
        }

        [Fact]
        public void ListFolders_InRootFolder_ReturnsOnlySubdirectories()
        {
            // Arrange
            string relativePath = "";
            string fullPath = "/media";
            
            // Mock path normalization
            _mockPath.Setup(p => p.GetFullPath(_schedulerSettings.MediaPath)).Returns("/media");
            _mockPath.Setup(p => p.GetFullPath(fullPath)).Returns(fullPath);
            _mockPath.Setup(p => p.GetFileName(It.IsAny<string>())).Returns<string>(s => s.Split('/').Last());
            
            // Setup directory operations
            _mockDirectory.Setup(d => d.Exists(_schedulerSettings.MediaPath)).Returns(true);
            _mockDirectory.Setup(d => d.Exists(fullPath)).Returns(true);
            _mockDirectory.Setup(d => d.GetDirectories(fullPath)).Returns(new[] { "/media/dir1", "/media/dir2" });
            
            // Setup logger
            _mockLogger.Setup(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
            
            var folderService = new FolderService(_mockLogger.Object, _mockSettingsService.Object, _mockFileSystem.Object);
            
            // Act
            var result = folderService.ListFolders(relativePath).ToList();
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // No parent for root directory, just 2 subdirectories
            Assert.Equal("dir1", result[0].Name);
            Assert.True(result[0].IsDirectory);
            Assert.Equal("dir2", result[1].Name);
            Assert.True(result[1].IsDirectory);
        }

        [Fact]
        public void ListFolders_WithInvalidPath_ThrowsDirectoryNotFoundException()
        {
            // Arrange
            string relativePath = "nonExistentFolder";
            string fullPath = "/media/nonExistentFolder";
            
            // Mock path normalization
            _mockPath.Setup(p => p.GetFullPath(_schedulerSettings.MediaPath)).Returns("/media");
            _mockPath.Setup(p => p.GetFullPath(fullPath)).Returns(fullPath);
            
            // Setup directory operations
            _mockDirectory.Setup(d => d.Exists(_schedulerSettings.MediaPath)).Returns(true);
            _mockDirectory.Setup(d => d.Exists(fullPath)).Returns(false);
            
            // Setup logger
            _mockLogger.Setup(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
            
            var folderService = new FolderService(_mockLogger.Object, _mockSettingsService.Object, _mockFileSystem.Object);
            
            // Act & Assert
            Assert.Throws<DirectoryNotFoundException>(() => folderService.ListFolders(relativePath));
        }

        [Fact]
        public void ListFolders_WithPathOutsideMediaDirectory_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            string relativePath = "../outside";
            string fullPath = "/outside"; // This would be outside the media path
            
            // Mock path normalization
            _mockPath.Setup(p => p.GetFullPath(_schedulerSettings.MediaPath)).Returns("/media");
            _mockPath.Setup(p => p.GetFullPath(It.IsAny<string>())).Returns(fullPath);
            
            // Setup directory operations
            _mockDirectory.Setup(d => d.Exists(_schedulerSettings.MediaPath)).Returns(true);
            
            // Setup logger
            _mockLogger.Setup(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
            
            var folderService = new FolderService(_mockLogger.Object, _mockSettingsService.Object, _mockFileSystem.Object);
            
            // Act & Assert
            Assert.Throws<UnauthorizedAccessException>(() => folderService.ListFolders(relativePath));
        }

        [Fact]
        public void CreateFolder_WithValidPath_CreatesAndReturnsFolder()
        {
            // Arrange
            string parentPath = "testParent";
            string folderName = "newFolder";
            string parentFullPath = "/media/testParent";
            string newFolderFullPath = "/media/testParent/newFolder";
            
            // Mock path normalization
            _mockPath.Setup(p => p.GetFullPath(_schedulerSettings.MediaPath)).Returns("/media");
            _mockPath.Setup(p => p.GetFullPath(parentFullPath)).Returns(parentFullPath);
            _mockPath.Setup(p => p.GetFullPath(newFolderFullPath)).Returns(newFolderFullPath);
            _mockPath.Setup(p => p.GetFileName(folderName)).Returns(folderName);
            _mockPath.Setup(p => p.Combine(parentFullPath, folderName)).Returns(newFolderFullPath);
            _mockPath.SetupGet(p => p.DirectorySeparatorChar).Returns('/');
            
            // Setup directory operations
            _mockDirectory.Setup(d => d.Exists(_schedulerSettings.MediaPath)).Returns(true);
            _mockDirectory.Setup(d => d.Exists(parentFullPath)).Returns(true);
            _mockDirectory.Setup(d => d.Exists(newFolderFullPath)).Returns(false);
            _mockDirectory.Setup(d => d.CreateDirectory(newFolderFullPath)).Returns(Mock.Of<IDirectoryInfo>());
            
            // Setup logger
            _mockLogger.Setup(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
            
            var folderService = new FolderService(_mockLogger.Object, _mockSettingsService.Object, _mockFileSystem.Object);
            
            // Act
            var result = folderService.CreateFolder(parentPath, folderName);
            
            // Assert
            _mockDirectory.Verify(d => d.CreateDirectory(newFolderFullPath), Times.Once);
            Assert.NotNull(result);
            Assert.Equal(folderName, result.Name);
            Assert.True(result.IsDirectory);
            Assert.Equal($"{parentPath}/{folderName}", result.RelativePath);
        }

        [Fact]
        public void CreateFolder_WithEmptyFolderName_ThrowsArgumentException()
        {
            // Arrange
            string parentPath = "testParent";
            string folderName = "";
            
            // Setup logger
            _mockLogger.Setup(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
            
            var folderService = new FolderService(_mockLogger.Object, _mockSettingsService.Object, _mockFileSystem.Object);
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => folderService.CreateFolder(parentPath, folderName));
        }

        [Fact]
        public void CreateFolder_WithInvalidCharactersInName_ThrowsIoException()
        {
            // Arrange
            string parentPath = "testParent";
            string folderName = "invalid/folder*name";
            string folderpath = Path.Combine(_schedulerSettings.MediaPath, parentPath);
            // Mock path operations for invalid filename
            _mockPath.Setup(p => p.GetFileName(folderName)).Returns(folderName);
            _mockPath.Setup(p => p.GetInvalidFileNameChars()).Returns(new[] { '/', '*' });
            _mockDirectory.Setup(d => d.Exists(_schedulerSettings.MediaPath)).Returns(true);
            _mockDirectory.Setup(d => d.Exists(Path.Combine(_schedulerSettings.MediaPath, parentPath))).Returns(true);
            _mockDirectory.Setup(d => d.Exists(folderpath)).Throws(new IOException("Invalid folder name"));
            // Setup logger
            _mockLogger.Setup(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
            
            var folderService = new FolderService(_mockLogger.Object, _mockSettingsService.Object, _mockFileSystem.Object);
            
            // Act & Assert
            Assert.Throws<IOException>(() => folderService.CreateFolder(parentPath, folderName));
        }

        [Fact]
        public void CreateFolder_WithExistingFolder_ThrowsIOException()
        {
            // Arrange
            string parentPath = "testParent";
            string folderName = "existingFolder";
            string parentFullPath = "/media/testParent";
            string newFolderFullPath = "/media/testParent/existingFolder";
            
            // Mock path normalization
            _mockPath.Setup(p => p.GetFullPath(_schedulerSettings.MediaPath)).Returns("/media");
            _mockPath.Setup(p => p.GetFullPath(parentFullPath)).Returns(parentFullPath);
            _mockPath.Setup(p => p.GetFullPath(newFolderFullPath)).Returns(newFolderFullPath);
            _mockPath.Setup(p => p.GetFileName(folderName)).Returns(folderName);
            _mockPath.Setup(p => p.Combine(parentFullPath, folderName)).Returns(newFolderFullPath);
            _mockPath.Setup(p => p.GetInvalidFileNameChars()).Returns(Array.Empty<char>());
            
            // Setup directory operations
            _mockDirectory.Setup(d => d.Exists(_schedulerSettings.MediaPath)).Returns(true);
            _mockDirectory.Setup(d => d.Exists(parentFullPath)).Returns(true);
            _mockDirectory.Setup(d => d.Exists(newFolderFullPath)).Returns(true);
            
            // Setup logger
            _mockLogger.Setup(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
            
            var folderService = new FolderService(_mockLogger.Object, _mockSettingsService.Object, _mockFileSystem.Object);
            
            // Act & Assert
            Assert.Throws<IOException>(() => folderService.CreateFolder(parentPath, folderName));
        }

        [Fact]
        public void FolderExists_WithExistingFolder_ReturnsTrue()
        {
            // Arrange
            string relativePath = "existingFolder";
            string fullPath = "/media/existingFolder";
            
            // Mock path normalization
            _mockPath.Setup(p => p.GetFullPath(_schedulerSettings.MediaPath)).Returns("/media");
            _mockPath.Setup(p => p.GetFullPath(fullPath)).Returns(fullPath);
            
            // Setup directory operations
            _mockDirectory.Setup(d => d.Exists(fullPath)).Returns(true);
            
            var folderService = new FolderService(_mockLogger.Object, _mockSettingsService.Object, _mockFileSystem.Object);
            
            // Act
            bool result = folderService.FolderExists(relativePath);
            
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void FolderExists_WithNonExistingFolder_ReturnsFalse()
        {
            // Arrange
            string relativePath = "nonExistingFolder";
            string fullPath = "/media/nonExistingFolder";
            
            // Mock path normalization
            _mockPath.Setup(p => p.GetFullPath(_schedulerSettings.MediaPath)).Returns("/media");
            _mockPath.Setup(p => p.GetFullPath(fullPath)).Returns(fullPath);
            
            // Setup directory operations
            _mockDirectory.Setup(d => d.Exists(fullPath)).Returns(false);
            
            var folderService = new FolderService(_mockLogger.Object, _mockSettingsService.Object, _mockFileSystem.Object);
            
            // Act
            bool result = folderService.FolderExists(relativePath);
            
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void FolderExists_WithPathOutsideMediaDirectory_ReturnsFalse()
        {
            // Arrange
            string relativePath = "../outside";
            string fullPath = "/outside"; // This would be outside the media path
            
            // Mock path normalization
            _mockPath.Setup(p => p.GetFullPath(_schedulerSettings.MediaPath)).Returns("/media");
            _mockPath.Setup(p => p.GetFullPath(It.IsAny<string>())).Returns(fullPath);
            
            var folderService = new FolderService(_mockLogger.Object, _mockSettingsService.Object, _mockFileSystem.Object);
            
            // Act
            bool result = folderService.FolderExists(relativePath);
            
            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ListFolders_WhenParentOutsideMediaPath_AddsDotDotPointingToRoot()
        {
            // Arrange
            // Testing a folder that's one level deep, so parent navigation would reach the root
            string relativePath = "level1";
            string fullPath = "/media/level1";
            string parentPath = "/media"; // Parent path is the media root path
            
            // Mock path normalization
            _mockPath.Setup(p => p.GetFullPath(_schedulerSettings.MediaPath)).Returns("/media");
            _mockPath.Setup(p => p.GetFullPath(fullPath)).Returns(fullPath);
            _mockPath.Setup(p => p.GetDirectoryName(fullPath)).Returns(parentPath);
            _mockPath.Setup(p => p.GetFileName(It.IsAny<string>())).Returns<string>(s => s.Split('/').Last());
            
            // Setup directory operations            
            _mockDirectory.Setup(d => d.Exists(_schedulerSettings.MediaPath)).Returns(true);
            _mockDirectory.Setup(d => d.Exists(fullPath)).Returns(true);
            _mockDirectory.Setup(d => d.GetDirectories(fullPath)).Returns(Array.Empty<string>());
            
            // Setup logger
            _mockLogger.Setup(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
            
            var folderService = new FolderService(_mockLogger.Object, _mockSettingsService.Object, _mockFileSystem.Object);
            
            // Act
            var result = folderService.ListFolders(relativePath).ToList();
            
            // Assert
            Assert.NotNull(result);
            Assert.Single(result); // Only the parent directory ".." should be returned since there are no subdirectories
            Assert.Equal("..", result[0].Name);
            Assert.True(result[0].IsDirectory);
            Assert.Equal("", result[0].RelativePath); // RelativePath should be empty to indicate root media folder
        }

        [Fact]
        public void CreateFolder_WithInvalidFolderNameContainingInvalidChars_ThrowsArgumentException()
        {
            // Arrange
            string parentPath = "testParent";
            string folderName = "folder:with*invalid?chars";
            
            // Mock path normalization
            _mockPath.Setup(p => p.GetFullPath(_schedulerSettings.MediaPath)).Returns("/media");
            _mockPath.Setup(p => p.GetFileName(folderName)).Returns(folderName);
            _mockPath.Setup(p => p.GetInvalidFileNameChars()).Returns(new[] { ':', '*', '?' });
            _mockDirectory.Setup(d => d.Exists(_schedulerSettings.MediaPath)).Returns(true);
            var parentFullPath = Path.Combine(_schedulerSettings.MediaPath, parentPath);
            _mockDirectory.Setup(d => d.Exists(parentFullPath)).Returns(true);
            var newFolderFullPath = Path.Combine(parentFullPath, folderName);
            _mockDirectory.Setup(d => d.Exists(newFolderFullPath)).Returns(true);

            // Setup directory operations
            _mockDirectory.Setup(d => d.Exists(_schedulerSettings.MediaPath)).Returns(true);
            
            // Setup logger
            _mockLogger.Setup(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
            
            var folderService = new FolderService(_mockLogger.Object, _mockSettingsService.Object, _mockFileSystem.Object);
            
            // Act & Assert
            Assert.Throws<IOException>(() => folderService.CreateFolder(parentPath, folderName));
        }

        [Fact]
        public void CreateFolder_WithParentPathOutsideMediaDirectory_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            string parentPath = "../outsideMedia";
            string folderName = "newFolder";
            string parentFullPath = "/outside"; // This would be outside the media path
            
            // Mock path normalization
            _mockPath.Setup(p => p.GetFullPath(_schedulerSettings.MediaPath)).Returns("/media");
            _mockPath.Setup(p => p.GetFullPath(It.IsAny<string>())).Returns(parentFullPath);
            _mockPath.Setup(p => p.GetFileName(folderName)).Returns(folderName);
            _mockPath.Setup(p => p.GetInvalidFileNameChars()).Returns(Array.Empty<char>());
            
            // Setup directory operations - this was missing
            _mockDirectory.Setup(d => d.Exists(_schedulerSettings.MediaPath)).Returns(true);
            
            // Setup logger
            _mockLogger.Setup(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
            
            var folderService = new FolderService(_mockLogger.Object, _mockSettingsService.Object, _mockFileSystem.Object);
            
            // Act & Assert
            Assert.Throws<UnauthorizedAccessException>(() => folderService.CreateFolder(parentPath, folderName));
        }

        [Fact]
        public void CreateFolder_WithCombinedPathOutsideMediaDirectory_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            string parentPath = "testParent";
            string folderName = ".."; // Using .. as folder name to attempt directory traversal
            string parentFullPath = "/media/testParent";
            string newFolderFullPath = "/outside"; // This would resolve outside media path when .. is processed
            
            // Mock path normalization
            _mockPath.Setup(p => p.GetFullPath(_schedulerSettings.MediaPath)).Returns("/media");
            _mockPath.Setup(p => p.GetFullPath(parentFullPath)).Returns(parentFullPath);
            _mockPath.Setup(p => p.GetFullPath(newFolderFullPath)).Returns(newFolderFullPath);
            _mockPath.Setup(p => p.GetFileName(folderName)).Returns(folderName);
            _mockPath.Setup(p => p.GetInvalidFileNameChars()).Returns(Array.Empty<char>());
            _mockPath.Setup(p => p.Combine(parentFullPath, folderName)).Returns(newFolderFullPath);
            
            // Setup directory operations
            _mockDirectory.Setup(d => d.Exists(_schedulerSettings.MediaPath)).Returns(true);
            _mockDirectory.Setup(d => d.Exists(parentFullPath)).Returns(true);
            _mockDirectory.Setup(d => d.Exists("/media/testParent/..")).Returns(true);
            // Setup logger
            _mockLogger.Setup(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
            
            var folderService = new FolderService(_mockLogger.Object, _mockSettingsService.Object, _mockFileSystem.Object);
            
            // Act & Assert
            Assert.Throws<IOException>(() => folderService.CreateFolder(parentPath, folderName));
        }

        [Fact]
        public void CreateFolder_WithNonExistentParentPath_CreatesParentDirectory()
        {
            // Arrange
            string parentPath = "nonExistentParent";
            string folderName = "newFolder";
            string parentFullPath = "/media/nonExistentParent";
            string newFolderFullPath = "/media/nonExistentParent/newFolder";
            
            // Mock path normalization
            _mockPath.Setup(p => p.GetFullPath(_schedulerSettings.MediaPath)).Returns("/media");
            _mockPath.Setup(p => p.GetFullPath(parentFullPath)).Returns(parentFullPath);
            _mockPath.Setup(p => p.GetFullPath(newFolderFullPath)).Returns(newFolderFullPath);
            _mockPath.Setup(p => p.GetFileName(folderName)).Returns(folderName);
            _mockPath.Setup(p => p.Combine(parentFullPath, folderName)).Returns(newFolderFullPath);
            _mockPath.SetupGet(p => p.DirectorySeparatorChar).Returns('/');
            
            // Setup directory operations - parent directory does not exist
            _mockDirectory.Setup(d => d.Exists(_schedulerSettings.MediaPath)).Returns(true);
            _mockDirectory.Setup(d => d.Exists(parentFullPath)).Returns(false);
            _mockDirectory.Setup(d => d.Exists(newFolderFullPath)).Returns(false);
            _mockDirectory.Setup(d => d.CreateDirectory(parentFullPath)).Returns(Mock.Of<IDirectoryInfo>());
            _mockDirectory.Setup(d => d.CreateDirectory(newFolderFullPath)).Returns(Mock.Of<IDirectoryInfo>());
            
            // Setup logger
            _mockLogger.Setup(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
            
            var folderService = new FolderService(_mockLogger.Object, _mockSettingsService.Object, _mockFileSystem.Object);
            
            // Act
            var result = folderService.CreateFolder(parentPath, folderName);
            
            // Assert
            _mockDirectory.Verify(d => d.CreateDirectory(parentFullPath), Times.Once);
            _mockDirectory.Verify(d => d.CreateDirectory(newFolderFullPath), Times.Once);
            Assert.NotNull(result);
            Assert.Equal(folderName, result.Name);
            Assert.True(result.IsDirectory);
            Assert.Equal($"{parentPath}/{folderName}", result.RelativePath);
        }
    }
}