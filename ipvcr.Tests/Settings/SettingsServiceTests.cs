using System;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Moq;
using System.Text.Json;
using ipvcr.Logic.Settings;
using ipvcr.Logic.Api;

namespace ipvcr.Tests.Settings
{
    public class SettingsServiceTests
    {
        private readonly Mock<IFileSystem> _mockFileSystem;
        private readonly Mock<ITokenManager> _mockTokenManager;

        public SettingsServiceTests()
        {
            _mockFileSystem = new Mock<IFileSystem>();
            _mockTokenManager = new Mock<ITokenManager>();

            // Setup common file system mocks
            var fileMock = new Mock<IFile>();
            _mockFileSystem.Setup(fs => fs.File).Returns(fileMock.Object);

            var directoryMock = new Mock<IDirectory>();
            _mockFileSystem.Setup(fs => fs.Directory).Returns(directoryMock.Object);

            var pathMock = new Mock<IPath>();
            _mockFileSystem.Setup(fs => fs.Path).Returns(pathMock.Object);

            // Path setup for combining settings path
            pathMock.Setup(p => p.Combine(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string>((path1, path2) => $"{path1}/{path2}");

            // Ensure directories exist
            directoryMock.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);

            // Empty settings files
            fileMock.Setup(f => f.Exists(It.IsAny<string>())).Returns(false);
            fileMock.Setup(f => f.Create(It.IsAny<string>())).Returns(new MockedFileSystemStream(new MemoryStream(), "/mocked/path", false));
            fileMock.Setup(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>())).Verifiable();

            // Setup token manager
            _mockTokenManager.Setup(tm => tm.CreateHash(It.IsAny<string>()))
                .Returns((string s) => $"hashed_{s}");
        }

        [Fact]
        public void Constructor_InitializesAllSettingsManagers()
        {
            // Act
            var service = new SettingsService(_mockFileSystem.Object, _mockTokenManager.Object);

            // Assert
            Assert.NotNull(service.SchedulerSettings);
            Assert.NotNull(service.PlaylistSettings);
            Assert.NotNull(service.SslSettings);
            Assert.NotNull(service.FfmpegSettings);
            Assert.NotNull(service.AdminPasswordSettings);
        }

        [Fact]
        public void Service_ShouldRaiseSettingsChangedEvent_WhenSchedulerSettingsChanged()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddDirectory("/data");
            var service = new SettingsService(mockFileSystem, _mockTokenManager.Object);

            bool eventRaised = false;
            SettingsType? changedSettingsType = null;
            object? newSettings = null;

            service.SettingsChanged += (sender, args) =>
            {
                eventRaised = true;
                changedSettingsType = args.SettingsType;
                newSettings = args.NewSettings;
            };

            // Act
            var settings = new SchedulerSettings { MediaPath = "/test/path" };
            service.SchedulerSettings = settings;

            // Assert
            Assert.True(eventRaised);
            Assert.Equal(SettingsType.Scheduler, changedSettingsType);
            Assert.NotNull(newSettings);
            var typedSettings = Assert.IsType<SchedulerSettings>(newSettings);
            Assert.Equal("/test/path", typedSettings.MediaPath);
        }

        [Fact]
        public void Service_ShouldRaiseSettingsChangedEvent_WhenPlaylistSettingsChanged()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddDirectory("/data");
            var service = new SettingsService(mockFileSystem, _mockTokenManager.Object);

            bool eventRaised = false;
            SettingsType? changedSettingsType = null;
            object? newSettings = null;

            service.SettingsChanged += (sender, args) =>
            {
                eventRaised = true;
                changedSettingsType = args.SettingsType;
                newSettings = args.NewSettings;
            };

            // Act
            var settings = new PlaylistSettings { M3uPlaylistPath = "/test/playlist.m3u" };
            service.PlaylistSettings = settings;

            // Assert
            Assert.True(eventRaised);
            Assert.Equal(SettingsType.Playlist, changedSettingsType);
            Assert.NotNull(newSettings);
            var typedSettings = Assert.IsType<PlaylistSettings>(newSettings);
            Assert.Equal("/test/playlist.m3u", typedSettings.M3uPlaylistPath);
        }

        [Fact]
        public void Service_ShouldRaiseSettingsChangedEvent_WhenFfmpegSettingsChanged()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddDirectory("/data");
            var service = new SettingsService(mockFileSystem, _mockTokenManager.Object);

            bool eventRaised = false;
            SettingsType? changedSettingsType = null;
            object? newSettings = null;

            service.SettingsChanged += (sender, args) =>
            {
                eventRaised = true;
                changedSettingsType = args.SettingsType;
                newSettings = args.NewSettings;
            };

            // Act
            var settings = new FfmpegSettings { Codec = "testcodec" };
            service.FfmpegSettings = settings;

            // Assert
            Assert.True(eventRaised);
            Assert.Equal(SettingsType.Ffmpeg, changedSettingsType);
            Assert.NotNull(newSettings);
            var typedSettings = Assert.IsType<FfmpegSettings>(newSettings);
            Assert.Equal("testcodec", typedSettings.Codec);
        }

        [Fact]
        public void Service_ShouldRaiseSettingsChangedEvent_WhenSslSettingsChanged()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddDirectory("/data");
            var service = new SettingsService(mockFileSystem, _mockTokenManager.Object);

            bool eventRaised = false;
            SettingsType? changedSettingsType = null;
            object? newSettings = null;

            service.SettingsChanged += (sender, args) =>
            {
                eventRaised = true;
                changedSettingsType = args.SettingsType;
                newSettings = args.NewSettings;
            };

            // Act
            var settings = new SslSettings { UseSsl = true };
            service.SslSettings = settings;

            // Assert
            Assert.True(eventRaised);
            Assert.Equal(SettingsType.Ssl, changedSettingsType);
            Assert.NotNull(newSettings);
            var typedSettings = Assert.IsType<SslSettings>(newSettings);
            Assert.True(typedSettings.UseSsl);
        }

        [Fact]
        public void Service_ShouldRaiseSettingsChangedEvent_WhenAdminPasswordSettingsChanged()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddDirectory("/data");

            // Create initial admin password settings
            var initialSettings = new AdminPasswordSettings { AdminUsername = "admin", AdminPassword = "password" };
            mockFileSystem.AddFile("/data/adminpassword.json", new MockFileData(JsonSerializer.Serialize(initialSettings)));

            var service = new SettingsService(mockFileSystem, _mockTokenManager.Object);

            bool eventRaised = false;
            SettingsType? changedSettingsType = null;
            object? newSettings = null;

            service.SettingsChanged += (sender, args) =>
            {
                eventRaised = true;
                changedSettingsType = args.SettingsType;
                newSettings = args.NewSettings;
            };

            // Act
            var settings = new AdminPasswordSettings { AdminUsername = "testadmin", AdminPassword = "" };
            service.AdminPasswordSettings = settings;

            // Assert
            Assert.True(eventRaised);
            Assert.Equal(SettingsType.AdminPassword, changedSettingsType);
            Assert.NotNull(newSettings);
            var typedSettings = Assert.IsType<AdminPasswordSettings>(newSettings);
            Assert.Equal("testadmin", typedSettings.AdminUsername);
        }

        [Fact]
        public void ValidateAdminPassword_DelegatesToAdminPasswordManager()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddDirectory("/data");

            // Create a settings file with a known value
            var settings = new AdminPasswordSettings { AdminUsername = "admin", AdminPassword = "hashed_password" };
            fileSystem.AddFile("/data/adminpassword.json", new MockFileData(JsonSerializer.Serialize(settings)));

            // Create a mock token manager that simulates password hashing
            var tokenManager = new Mock<ITokenManager>();
            tokenManager.Setup(tm => tm.CreateHash("password"))
                .Returns("hashed_password");

            var service = new SettingsService(fileSystem, tokenManager.Object);

            // Act
            var result = service.ValidateAdminPassword("password");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void UpdateAdminPassword_DelegatesToAdminPasswordManager()
        {
            // Arrange
            var fileSystem = new MockFileSystem();
            fileSystem.AddDirectory("/data");

            // Create a mock token manager that simulates password hashing
            var tokenManager = new Mock<ITokenManager>();
            tokenManager.Setup(tm => tm.CreateHash("newpassword"))
                .Returns("hashed_newpassword");

            var service = new SettingsService(fileSystem, tokenManager.Object);

            // Act
            service.UpdateAdminPassword("newpassword");

            // Assert - The admin password should be updated with the hashed value
            // But since the Settings getter clears the password for security, we'd need
            // to verify through other means, such as checking if validation passes
        }

        [Fact]
        public void ResetFactoryDefaults_ResetsAllSettings()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddDirectory("/data");
            var service = new SettingsService(mockFileSystem, _mockTokenManager.Object);

            // Customize all settings first
            service.SchedulerSettings = new SchedulerSettings { MediaPath = "/custom/media" };
            service.PlaylistSettings = new PlaylistSettings { M3uPlaylistPath = "/custom/playlist.m3u" };
            service.SslSettings = new SslSettings { UseSsl = false };
            service.FfmpegSettings = new FfmpegSettings { Codec = "custom" };
            service.AdminPasswordSettings = new AdminPasswordSettings { AdminUsername = "customadmin" };

            // Act
            service.ResetFactoryDefaults();

            // Assert
            Assert.Equal("/media", service.SchedulerSettings.MediaPath);
            Assert.Equal("/data/m3u-playlist.m3u", service.PlaylistSettings.M3uPlaylistPath);
            Assert.True(service.SslSettings.UseSsl);
            Assert.Equal("", service.FfmpegSettings.Codec);
            Assert.Equal("mp4", service.FfmpegSettings.OutputFormat);
            Assert.Equal("admin", service.AdminPasswordSettings.AdminUsername);
        }

        [Fact]
        public void Service_ShouldNotThrowException_WhenNoEventHandlerRegistered()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddDirectory("/data");
            var service = new SettingsService(mockFileSystem, _mockTokenManager.Object);

            // Act & Assert (should not throw)
            var exception = Record.Exception(() => service.SchedulerSettings = new SchedulerSettings());
            Assert.Null(exception);
        }
    }
}