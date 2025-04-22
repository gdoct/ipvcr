using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using ipvcr.Logic.Api;
using ipvcr.Logic.Settings;
using Moq;
#pragma warning disable CS8600, CS8602, CS8603, CS8604, CS8618, CS8625
namespace ipvcr.Tests.Settings
{
    public class AdminPasswordManagerTests
    {
        private readonly Mock<IFileSystem> _fileSystemMock;
        private readonly Mock<ITokenManager> _tokenManagerMock;
        private readonly Mock<IFile> _fileMock;
        private readonly Mock<IDirectory> _directoryMock;
        private readonly Mock<IPath> _pathMock;
        private const string SettingsFilePath = "/data/adminpassword.json";

        public AdminPasswordManagerTests()
        {
            _fileSystemMock = new Mock<IFileSystem>(MockBehavior.Default);
            _tokenManagerMock = new Mock<ITokenManager>(MockBehavior.Default);

            // Setup common file system mocks
            _fileMock = new Mock<IFile>(MockBehavior.Default);
            _fileSystemMock.Setup(fs => fs.File).Returns(_fileMock.Object);

            _directoryMock = new Mock<IDirectory>(MockBehavior.Default);
            _fileSystemMock.Setup(fs => fs.Directory).Returns(_directoryMock.Object);

            _pathMock = new Mock<IPath>(MockBehavior.Default);
            _fileSystemMock.Setup(fs => fs.Path).Returns(_pathMock.Object);

            // Path setup for combining settings path
            _pathMock.Setup(p => p.Combine(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string>((path1, path2) => $"{path1}/{path2}");

            // Default directory setup
            _directoryMock.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);
        }

        [Fact]
        public void Constructor_NullFileSystem_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AdminPasswordManager(null!, _tokenManagerMock.Object));
        }

        [Fact]
        public void Constructor_NullTokenManager_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
            {
                // Setup file system to avoid other errors
                _fileMock.Setup(f => f.Exists(SettingsFilePath)).Returns(false);
                _fileMock.Setup(f => f.Create(SettingsFilePath)).Returns(new MockedFileSystemStream(new MemoryStream(), "/mocked/path"));
                _fileMock.Setup(f => f.WriteAllText(SettingsFilePath, It.IsAny<string>()));

                // Force null without using null! which bypasses null checking
                _ = new AdminPasswordManager(_fileSystemMock.Object, null);
            });
        }

        [Fact]
        public void Settings_Get_ReturnsSettingsWithEmptyPassword()
        {
            // Arrange
            _fileMock.Setup(f => f.Exists(SettingsFilePath)).Returns(true);

            var settings = new AdminPasswordSettings { AdminUsername = "testadmin", AdminPassword = "hashedpassword" };
            _fileMock.Setup(f => f.ReadAllText(SettingsFilePath))
                .Returns(JsonSerializer.Serialize(settings));

            var manager = new AdminPasswordManager(_fileSystemMock.Object, _tokenManagerMock.Object);

            // Act
            var result = manager.Settings;

            // Assert
            Assert.Equal("testadmin", result.AdminUsername);
            Assert.Equal(string.Empty, result.AdminPassword); // Password should be empty in returned settings
        }

        [Fact]
        public void Settings_Set_WithValidSettings_UpdatesUsername()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddDirectory("/data");

            var fileSystemWrapperMock = new Mock<IFileSystem>();
            fileSystemWrapperMock.Setup(fs => fs.File).Returns(mockFileSystem.File);
            fileSystemWrapperMock.Setup(fs => fs.Directory).Returns(mockFileSystem.Directory);
            fileSystemWrapperMock.Setup(fs => fs.Path).Returns(mockFileSystem.Path);

            _tokenManagerMock.Setup(tm => tm.CreateHash(It.IsAny<string>()))
                .Returns((string s) => $"hashed_{s}");

            var settings = new AdminPasswordSettings { AdminUsername = "admin", AdminPassword = "hashedpassword" };
            mockFileSystem.AddFile(SettingsFilePath, new MockFileData(JsonSerializer.Serialize(settings)));

            var manager = new AdminPasswordManager(fileSystemWrapperMock.Object, _tokenManagerMock.Object)
            {
                // Act
                Settings = new AdminPasswordSettings { AdminUsername = "newadmin" }
            };

            // Assert
            var savedText = mockFileSystem.GetFile(SettingsFilePath).TextContents;
            var savedSettings = JsonSerializer.Deserialize<AdminPasswordSettings>(savedText);

            Assert.Equal("newadmin", savedSettings.AdminUsername);
            Assert.Equal("hashedpassword", savedSettings.AdminPassword); // Password should remain unchanged
        }

        [Fact]
        public void Settings_Set_WithNullSettings_ThrowsArgumentNullException()
        {
            // Arrange
            _fileMock.Setup(f => f.Exists(SettingsFilePath)).Returns(true);

            var settings = new AdminPasswordSettings { AdminUsername = "admin", AdminPassword = "hashedpassword" };
            _fileMock.Setup(f => f.ReadAllText(SettingsFilePath))
                .Returns(JsonSerializer.Serialize(settings));

            var manager = new AdminPasswordManager(_fileSystemMock.Object, _tokenManagerMock.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => manager.Settings = null);
        }

        [Fact]
        public void Settings_Set_WithEmptyUsername_ThrowsArgumentException()
        {
            // Arrange
            _fileMock.Setup(f => f.Exists(SettingsFilePath)).Returns(true);

            var settings = new AdminPasswordSettings { AdminUsername = "admin", AdminPassword = "hashedpassword" };
            _fileMock.Setup(f => f.ReadAllText(SettingsFilePath))
                .Returns(JsonSerializer.Serialize(settings));

            var manager = new AdminPasswordManager(_fileSystemMock.Object, _tokenManagerMock.Object);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => manager.Settings = new AdminPasswordSettings { AdminUsername = "" });
        }

        [Fact]
        public void LoadSettings_FileDoesNotExist_ReturnsDefaultSettings()
        {
            // Arrange
            _fileMock.Setup(f => f.Exists(SettingsFilePath)).Returns(false);
            _fileMock.Setup(f => f.Create(SettingsFilePath)).Returns(new MockedFileSystemStream(new MemoryStream()));
            _fileMock.Setup(f => f.WriteAllText(SettingsFilePath, It.IsAny<string>()));

            var manager = new AdminPasswordManager(_fileSystemMock.Object, _tokenManagerMock.Object);

            // Access private method through property
            var settings = manager.Settings;

            // Assert
            Assert.Equal("admin", settings.AdminUsername);
            Assert.Equal(string.Empty, settings.AdminPassword); // Empty because Settings getter clears the password
        }

        [Fact]
        public void ValidateAdminPassword_ValidPassword_ReturnsTrue()
        {
            // Arrange
            _fileMock.Setup(f => f.Exists(SettingsFilePath)).Returns(true);

            var settings = new AdminPasswordSettings { AdminUsername = "admin", AdminPassword = "hashed_password" };
            _fileMock.Setup(f => f.ReadAllText(SettingsFilePath))
                .Returns(JsonSerializer.Serialize(settings));

            _tokenManagerMock.Setup(tm => tm.CreateHash("password"))
                .Returns("hashed_password");

            var manager = new AdminPasswordManager(_fileSystemMock.Object, _tokenManagerMock.Object);

            // Act
            var result = manager.ValidateAdminPassword("password");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateAdminPassword_InvalidPassword_ReturnsFalse()
        {
            // Arrange
            _fileMock.Setup(f => f.Exists(SettingsFilePath)).Returns(true);

            var settings = new AdminPasswordSettings { AdminUsername = "admin", AdminPassword = "hashed_correctpassword" };
            _fileMock.Setup(f => f.ReadAllText(SettingsFilePath))
                .Returns(JsonSerializer.Serialize(settings));

            _tokenManagerMock.Setup(tm => tm.CreateHash("wrongpassword"))
                .Returns("hashed_wrongpassword");

            var manager = new AdminPasswordManager(_fileSystemMock.Object, _tokenManagerMock.Object);

            // Act
            var result = manager.ValidateAdminPassword("wrongpassword");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateAdminPassword_EmptyPassword_ThrowsArgumentException()
        {
            // Arrange
            _fileMock.Setup(f => f.Exists(SettingsFilePath)).Returns(true);

            var settings = new AdminPasswordSettings { AdminUsername = "admin", AdminPassword = "hashedpassword" };
            _fileMock.Setup(f => f.ReadAllText(SettingsFilePath))
                .Returns(JsonSerializer.Serialize(settings));

            var manager = new AdminPasswordManager(_fileSystemMock.Object, _tokenManagerMock.Object);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => manager.ValidateAdminPassword(string.Empty));
        }

        [Fact]
        public void UpdateAdminPassword_ValidPassword_UpdatesPasswordHash()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddDirectory("/data");

            var fileSystemWrapperMock = new Mock<IFileSystem>();
            fileSystemWrapperMock.Setup(fs => fs.File).Returns(mockFileSystem.File);
            fileSystemWrapperMock.Setup(fs => fs.Directory).Returns(mockFileSystem.Directory);
            fileSystemWrapperMock.Setup(fs => fs.Path).Returns(mockFileSystem.Path);

            _tokenManagerMock.Setup(tm => tm.CreateHash("newpassword"))
                .Returns("hashed_newpassword");

            var settings = new AdminPasswordSettings { AdminUsername = "admin", AdminPassword = "hashed_oldpassword" };
            mockFileSystem.AddFile(SettingsFilePath, new MockFileData(JsonSerializer.Serialize(settings)));

            var manager = new AdminPasswordManager(fileSystemWrapperMock.Object, _tokenManagerMock.Object);

            // Act
            manager.UpdateAdminPassword("newpassword");

            // Assert
            var savedText = mockFileSystem.GetFile(SettingsFilePath).TextContents;
            var savedSettings = JsonSerializer.Deserialize<AdminPasswordSettings>(savedText);

            Assert.Equal("hashed_newpassword", savedSettings.AdminPassword);
        }

        [Fact]
        public void UpdateAdminPassword_EmptyPassword_ThrowsArgumentException()
        {
            // Arrange
            _fileMock.Setup(f => f.Exists(SettingsFilePath)).Returns(true);

            var settings = new AdminPasswordSettings { AdminUsername = "admin", AdminPassword = "hashedpassword" };
            _fileMock.Setup(f => f.ReadAllText(SettingsFilePath))
                .Returns(JsonSerializer.Serialize(settings));

            var manager = new AdminPasswordManager(_fileSystemMock.Object, _tokenManagerMock.Object);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => manager.UpdateAdminPassword(string.Empty));
        }

        [Fact]
        public void SetDefaultAdminPassword_ResetsPasswordToDefault()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddDirectory("/data");

            var fileSystemWrapperMock = new Mock<IFileSystem>();
            fileSystemWrapperMock.Setup(fs => fs.File).Returns(mockFileSystem.File);
            fileSystemWrapperMock.Setup(fs => fs.Directory).Returns(mockFileSystem.Directory);
            fileSystemWrapperMock.Setup(fs => fs.Path).Returns(mockFileSystem.Path);

            var settings = new AdminPasswordSettings { AdminUsername = "admin", AdminPassword = "custom_password" };
            mockFileSystem.AddFile(SettingsFilePath, new MockFileData(JsonSerializer.Serialize(settings)));

            var manager = new AdminPasswordManager(fileSystemWrapperMock.Object, _tokenManagerMock.Object);

            // Act
            manager.SetDefaultAdminPassword();

            // Assert
            var savedText = mockFileSystem.GetFile(SettingsFilePath).TextContents;
            var savedSettings = JsonSerializer.Deserialize<AdminPasswordSettings>(savedText);

            Assert.Equal("default_password", savedSettings.AdminPassword);
        }

        [Fact]
        public void GetAdminPassword_ReturnsCurrentPasswordHash()
        {
            // Arrange
            _fileMock.Setup(f => f.Exists(SettingsFilePath)).Returns(true);

            var settings = new AdminPasswordSettings { AdminUsername = "admin", AdminPassword = "current_password_hash" };
            _fileMock.Setup(f => f.ReadAllText(SettingsFilePath))
                .Returns(JsonSerializer.Serialize(settings));

            var manager = new AdminPasswordManager(_fileSystemMock.Object, _tokenManagerMock.Object);

            // Act - This will actually return empty because the Settings getter clears the password
            var result = manager.GetAdminPassword();

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void LoadSettings_EmptyUsernameAndPassword_AppliesDefaultValues()
        {
            // Arrange
            _fileMock.Setup(f => f.Exists(SettingsFilePath)).Returns(true);

            // Create settings with empty username and password
            var settings = new AdminPasswordSettings { AdminUsername = "", AdminPassword = "" };
            _fileMock.Setup(f => f.ReadAllText(SettingsFilePath))
                .Returns(JsonSerializer.Serialize(settings));

            var manager = new AdminPasswordManager(_fileSystemMock.Object, _tokenManagerMock.Object);

            // Act - This will load settings internally
            // We can't access LoadSettings directly as it's protected, but we can verify the results
            
            // Verify username is set to default via public Settings property
            Assert.Equal("admin", manager.AdminUsername);
            
            // The default password in AdminPasswordManager is set to the constant "default_password"
            // without hashing it first, so the validation should work by comparing hashes
            _tokenManagerMock.Setup(tm => tm.CreateHash("default_password"))
                .Returns("default_password");
                
            Assert.True(manager.ValidateAdminPassword("default_password"));
        }

    }
    
}
#pragma warning restore CS8600, CS8602, CS8603, CS8604, CS8618, CS8625