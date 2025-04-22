using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using ipvcr.Logic.Settings;
using Moq;

#pragma warning disable CS8600, CS8602, CS8603, CS8604, CS8618, CS8625
namespace ipvcr.Tests.Settings
{
    public class BaseSettingsManagerTests
    {
        private class TestSettings
        {
            public string TestProperty { get; set; } = "DefaultValue";
            public int TestNumber { get; set; } = 42;
        }

        // This is a special implementation that doesn't call LoadSettings in the constructor
        [ExcludeFromCodeCoverage]
        private class TestSettingsManagerMinimal : BaseSettingsManager<TestSettings>
        {
            public TestSettingsManagerMinimal(IFileSystem filesystem)
                : base(filesystem, "testsettings.json", "/data")
            {
                // Don't call LoadSettings in constructor for specific tests
                _settings = new TestSettings();
            }

            public new void SaveSettingsToFile(string jsonSettings) => base.SaveSettingsToFile(jsonSettings);
        }

        // Regular implementation for most tests
        [ExcludeFromCodeCoverage]
        private class TestSettingsManager : BaseSettingsManager<TestSettings>
        {
            public TestSettingsManager(IFileSystem filesystem) : base(filesystem, "testsettings.json", "/data")
            {
            }

            // Expose protected methods for testing
            public new TestSettings LoadSettings() => base.LoadSettings();
            public new void SaveSettings(TestSettings settings) => base.SaveSettings(settings);
            public new void SaveSettingsToFile(string jsonSettings) => base.SaveSettingsToFile(jsonSettings);
            public new void RaiseSettingsChanged(TestSettings settings) => base.RaiseSettingsChanged(settings);
        }

        // Special implementation just for the file creation test
        [ExcludeFromCodeCoverage]
        private class TestSettingsManagerNoFileOps : BaseSettingsManager<TestSettings>
        {
            public TestSettingsManagerNoFileOps(IFileSystem filesystem)
                : base(filesystem, "testsettings.json", "/data")
            {
                // Override base constructor behavior that loads settings
                _settings = new TestSettings();
            }

            public void TestSaveSettingsToFile(string json)
            {
                // Direct access to the protected method
                SaveSettingsToFile(json);
            }
        }

        private readonly MockFileSystem _mockFileSystem;
        private const string SettingsFilePath = "/data/testsettings.json";

        public BaseSettingsManagerTests()
        {
            _mockFileSystem = new MockFileSystem();
            _mockFileSystem.AddDirectory("/data");
        }

        [Fact]
        public void Constructor_NullFileSystem_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TestSettingsManager(null!));
        }

        [Fact]
        public void Constructor_LoadsExistingSettings()
        {
            // Arrange
            var expectedSettings = new TestSettings { TestProperty = "ExistingValue", TestNumber = 100 };
            _mockFileSystem.AddFile(SettingsFilePath, new MockFileData(JsonSerializer.Serialize(expectedSettings)));

            // Act
            var manager = new TestSettingsManager(_mockFileSystem);

            // Assert
            var settings = manager.Settings;
            Assert.Equal("ExistingValue", settings.TestProperty);
            Assert.Equal(100, settings.TestNumber);
        }

        [Fact]
        public void Constructor_CreatesDefaultSettingsIfFileDoesNotExist()
        {
            // Act
            var manager = new TestSettingsManager(_mockFileSystem);

            // Assert
            var settings = manager.Settings;
            Assert.Equal("DefaultValue", settings.TestProperty);
            Assert.Equal(42, settings.TestNumber);

            // Verify the settings file was created
            Assert.True(_mockFileSystem.FileExists(SettingsFilePath));

            // Verify the content is the serialized default settings
            var fileContent = _mockFileSystem.GetFile(SettingsFilePath).TextContents;
            var deserializedSettings = JsonSerializer.Deserialize<TestSettings>(fileContent);
            Assert.Equal("DefaultValue", deserializedSettings.TestProperty);
            Assert.Equal(42, deserializedSettings.TestNumber);
        }

        [Fact]
        public void LoadSettings_HandlesEmptyJsonFile()
        {
            // Arrange
            _mockFileSystem.AddFile(SettingsFilePath, new MockFileData(""));
            var manager = new TestSettingsManager(_mockFileSystem);

            // Act
            var settings = manager.LoadSettings();

            // Assert
            Assert.NotNull(settings);
            Assert.Equal("DefaultValue", settings.TestProperty);
        }

        [Fact]
        public void LoadSettings_HandlesUnauthorizedAccess()
        {
            // Arrange - Setup for a new TestSettingsManager instance that throws UnauthorizedAccessException
            var directoryMock = new Mock<IDirectory>();
            directoryMock.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);

            var pathMock = new Mock<IPath>();
            pathMock.Setup(p => p.Combine(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string>((p1, p2) => $"{p1}/{p2}");

            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(fs => fs.Directory).Returns(directoryMock.Object);
            fileSystemMock.Setup(fs => fs.Path).Returns(pathMock.Object);

            var fileMock = new Mock<IFile>();
            fileMock.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            fileMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Throws(new UnauthorizedAccessException("Test UnauthorizedAccessException"));
            fileSystemMock.Setup(fs => fs.File).Returns(fileMock.Object);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => new TestSettingsManager(fileSystemMock.Object));
            Assert.Equal("The settings file is not readable.", exception.Message);
        }

        [Fact]
        public void LoadSettings_HandlesInvalidJson()
        {
            // Arrange - Setup a file with invalid JSON content
            _mockFileSystem.AddFile(SettingsFilePath, new MockFileData("invalid json"));

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => new TestSettingsManager(_mockFileSystem));
            Assert.Equal("The settings file contains invalid JSON.", exception.Message);
        }

        [Fact]
        public void LoadSettings_HandlesIOException()
        {
            // Arrange - Setup for a new TestSettingsManager instance that throws IOException
            var directoryMock = new Mock<IDirectory>();
            directoryMock.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);

            var pathMock = new Mock<IPath>();
            pathMock.Setup(p => p.Combine(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string>((p1, p2) => $"{p1}/{p2}");

            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(fs => fs.Directory).Returns(directoryMock.Object);
            fileSystemMock.Setup(fs => fs.Path).Returns(pathMock.Object);

            var fileMock = new Mock<IFile>();
            fileMock.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            fileMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Throws(new IOException("Test IOException"));
            fileSystemMock.Setup(fs => fs.File).Returns(fileMock.Object);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => new TestSettingsManager(fileSystemMock.Object));
            Assert.Contains("An error occurred while reading the settings file", exception.Message);
            Assert.IsType<IOException>(exception.InnerException);
        }

        [Fact]
        public void Settings_Set_SavesAndRaisesEvent()
        {
            // Arrange
            var manager = new TestSettingsManager(_mockFileSystem);
            bool eventRaised = false;
            TestSettings eventSettings = null;

            manager.SettingsChanged += (sender, args) =>
            {
                eventRaised = true;
                eventSettings = args.NewSettings;
            };

            var newSettings = new TestSettings { TestProperty = "UpdatedValue", TestNumber = 99 };

            // Act
            manager.Settings = newSettings;

            // Assert
            Assert.True(eventRaised);
            Assert.Equal("UpdatedValue", eventSettings.TestProperty);
            Assert.Equal(99, eventSettings.TestNumber);

            // Verify the settings file was updated
            var fileContent = _mockFileSystem.GetFile(SettingsFilePath).TextContents;
            var deserializedSettings = JsonSerializer.Deserialize<TestSettings>(fileContent);
            Assert.Equal("UpdatedValue", deserializedSettings.TestProperty);
            Assert.Equal(99, deserializedSettings.TestNumber);
        }

        [Fact]
        public void Settings_Set_NullValue_ThrowsArgumentNullException()
        {
            // Arrange
            var manager = new TestSettingsManager(_mockFileSystem);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => manager.Settings = null);
        }

        [Fact]
        public void SaveSettingsToFile_HandlesUnauthorizedAccess()
        {
            // Arrange
            var fileSystemMock = new Mock<IFileSystem>();
            var fileMock = new Mock<IFile>();
            fileSystemMock.Setup(fs => fs.File).Returns(fileMock.Object);

            fileMock.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            fileMock.Setup(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new UnauthorizedAccessException());

            var manager = new TestSettingsManager(fileSystemMock.Object);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => manager.SaveSettingsToFile("{}"));
        }

        [Fact]
        public void SaveSettingsToFile_HandlesIOException()
        {
            // Arrange
            var fileSystemMock = new Mock<IFileSystem>();
            var fileMock = new Mock<IFile>();
            fileSystemMock.Setup(fs => fs.File).Returns(fileMock.Object);

            fileMock.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            fileMock.Setup(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new IOException());

            var manager = new TestSettingsManager(fileSystemMock.Object);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => manager.SaveSettingsToFile("{}"));
        }

        [Fact]
        public void SaveSettingsToFile_CreatesFileIfItDoesNotExist()
        {
            // Using real MockFileSystem instead of strict Moq mocks
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddDirectory("/data");

            // Use the regular implementation but don't construct it yet
            var manager = new TestSettingsManager(mockFileSystem);

            // Verify the file was created during constructor
            Assert.True(mockFileSystem.FileExists("/data/testsettings.json"));

            // Get the content of the file
            string initialContent = mockFileSystem.GetFile("/data/testsettings.json").TextContents;

            // Now change the content and call SaveSettingsToFile manually
            mockFileSystem.File.Delete("/data/testsettings.json");
            Assert.False(mockFileSystem.FileExists("/data/testsettings.json"));

            // Call the method under test
            manager.SaveSettingsToFile("{}");

            // Verify the file was created again
            Assert.True(mockFileSystem.FileExists("/data/testsettings.json"));

            // Verify the content has changed
            string newContent = mockFileSystem.GetFile("/data/testsettings.json").TextContents;
            Assert.Equal("{}", newContent);
            Assert.NotEqual(initialContent, newContent);
        }
    }
}
#pragma warning restore CS8600, CS8602, CS8603, CS8604, CS8618, CS8625