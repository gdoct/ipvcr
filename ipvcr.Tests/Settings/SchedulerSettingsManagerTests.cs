using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using ipvcr.Logic.Settings;
using Moq;
using Xunit;
#pragma warning disable CS8600, CS8602, CS8603, CS8604, CS8618, CS8625
namespace ipvcr.Tests.Settings
{
    public class SchedulerSettingsManagerTests
    {
        private readonly MockFileSystem _mockFileSystem;
        private const string SettingsFilePath = "/data/settings.json";

        public SchedulerSettingsManagerTests()
        {
            _mockFileSystem = new MockFileSystem();
            _mockFileSystem.AddDirectory("/data");
        }

        [Fact]
        public void Constructor_NullFileSystem_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SchedulerSettingsManager(null));
        }

        [Fact]
        public void Constructor_LoadsExistingSettings()
        {
            // Arrange
            var expectedSettings = new SchedulerSettings
            {
                MediaPath = "/custom/media",
                DataPath = "/custom/data",
                RemoveTaskAfterExecution = false
            };
            _mockFileSystem.AddFile(SettingsFilePath, new MockFileData(JsonSerializer.Serialize(expectedSettings)));

            // Act
            var manager = new SchedulerSettingsManager(_mockFileSystem);

            // Assert
            var settings = manager.Settings;
            Assert.Equal("/custom/media", settings.MediaPath);
            Assert.Equal("/custom/data", settings.DataPath);
            Assert.False(settings.RemoveTaskAfterExecution);
        }

        [Fact]
        public void Constructor_CreatesDefaultSettingsIfFileDoesNotExist()
        {
            // Act
            var manager = new SchedulerSettingsManager(_mockFileSystem);

            // Assert
            var settings = manager.Settings;
            Assert.Equal("/media", settings.MediaPath);
            Assert.Equal("/data", settings.DataPath);
            Assert.True(settings.RemoveTaskAfterExecution);

            // Verify the settings file was created
            Assert.True(_mockFileSystem.FileExists(SettingsFilePath));

            // Verify the content is the serialized default settings
            var fileContent = _mockFileSystem.GetFile(SettingsFilePath).TextContents;
            var deserializedSettings = JsonSerializer.Deserialize<SchedulerSettings>(fileContent);
            Assert.Equal("/media", deserializedSettings.MediaPath);
            Assert.Equal("/data", deserializedSettings.DataPath);
            Assert.True(deserializedSettings.RemoveTaskAfterExecution);
        }

        [Fact]
        public void Settings_Set_SavesAndUpdatesFile()
        {
            // Arrange
            var manager = new SchedulerSettingsManager(_mockFileSystem);
            var newSettings = new SchedulerSettings
            {
                MediaPath = "/updated/media",
                DataPath = "/updated/data",
                RemoveTaskAfterExecution = false
            };

            bool eventRaised = false;
            SchedulerSettings eventSettings = null;

            manager.SettingsChanged += (sender, args) =>
            {
                eventRaised = true;
                eventSettings = args.NewSettings;
            };

            // Act
            manager.Settings = newSettings;

            // Assert
            Assert.True(eventRaised);
            Assert.Equal("/updated/media", eventSettings.MediaPath);
            Assert.Equal("/updated/data", eventSettings.DataPath);
            Assert.False(eventSettings.RemoveTaskAfterExecution);

            // Verify the settings file was updated
            var fileContent = _mockFileSystem.GetFile(SettingsFilePath).TextContents;
            var deserializedSettings = JsonSerializer.Deserialize<SchedulerSettings>(fileContent);
            Assert.Equal("/updated/media", deserializedSettings.MediaPath);
            Assert.Equal("/updated/data", deserializedSettings.DataPath);
            Assert.False(deserializedSettings.RemoveTaskAfterExecution);
        }

        [Fact]
        public void Settings_Set_NullValue_ThrowsArgumentNullException()
        {
            // Arrange
            var manager = new SchedulerSettingsManager(_mockFileSystem);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => manager.Settings = null);
        }
    }
}
#pragma warning restore CS8600, CS8602, CS8603, CS8604, CS8618, CS8625