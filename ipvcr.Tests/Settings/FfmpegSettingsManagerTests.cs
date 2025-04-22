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
    public class FfmpegSettingsManagerTests
    {
        private readonly MockFileSystem _mockFileSystem;
        private const string SettingsFilePath = "/data/ffmpeg-settings.json";

        public FfmpegSettingsManagerTests()
        {
            _mockFileSystem = new MockFileSystem();
            _mockFileSystem.AddDirectory("/data");
        }

        [Fact]
        public void Constructor_NullFileSystem_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FfmpegSettingsManager(null));
        }

        [Fact]
        public void Constructor_LoadsExistingSettings()
        {
            // Arrange
            var expectedSettings = new FfmpegSettings
            {
                Codec = "testcodec",
                AudioCodec = "testaudiocodec",
                VideoBitrate = "2500k"
            };
            _mockFileSystem.AddFile(SettingsFilePath, new MockFileData(JsonSerializer.Serialize(expectedSettings)));

            // Act
            var manager = new FfmpegSettingsManager(_mockFileSystem);

            // Assert
            var settings = manager.Settings;
            Assert.Equal("testcodec", settings.Codec);
            Assert.Equal("testaudiocodec", settings.AudioCodec);
            Assert.Equal("2500k", settings.VideoBitrate);
        }

        [Fact]
        public void Constructor_CreatesDefaultSettingsIfFileDoesNotExist()
        {
            // Act
            var manager = new FfmpegSettingsManager(_mockFileSystem);

            // Assert
            var settings = manager.Settings;
            Assert.Equal("", settings.Codec);
            Assert.Equal("", settings.AudioCodec);
            Assert.Equal("mp4", settings.OutputFormat);

            // Verify the settings file was created
            Assert.True(_mockFileSystem.FileExists(SettingsFilePath));

            // Verify the content is the serialized default settings
            var fileContent = _mockFileSystem.GetFile(SettingsFilePath).TextContents;
            var deserializedSettings = JsonSerializer.Deserialize<FfmpegSettings>(fileContent);
            Assert.Equal("mp4", deserializedSettings.OutputFormat);
        }

        [Fact]
        public void Settings_Set_SavesAndUpdatesFile()
        {
            // Arrange
            var manager = new FfmpegSettingsManager(_mockFileSystem);
            var newSettings = new FfmpegSettings
            {
                Codec = "h265",
                AudioCodec = "opus",
                Resolution = "3840x2160"
            };

            bool eventRaised = false;
            FfmpegSettings eventSettings = null;

            manager.SettingsChanged += (sender, args) =>
            {
                eventRaised = true;
                eventSettings = args.NewSettings;
            };

            // Act
            manager.Settings = newSettings;

            // Assert
            Assert.True(eventRaised);
            Assert.Equal("h265", eventSettings.Codec);
            Assert.Equal("opus", eventSettings.AudioCodec);
            Assert.Equal("3840x2160", eventSettings.Resolution);

            // Verify the settings file was updated
            var fileContent = _mockFileSystem.GetFile(SettingsFilePath).TextContents;
            var deserializedSettings = JsonSerializer.Deserialize<FfmpegSettings>(fileContent);
            Assert.Equal("h265", deserializedSettings.Codec);
            Assert.Equal("opus", deserializedSettings.AudioCodec);
            Assert.Equal("3840x2160", deserializedSettings.Resolution);
        }

        [Fact]
        public void Settings_Set_NullValue_ThrowsArgumentNullException()
        {
            // Arrange
            var manager = new FfmpegSettingsManager(_mockFileSystem);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => manager.Settings = null);
        }
    }
}
#pragma warning restore CS8600, CS8602, CS8603, CS8604, CS8618, CS8625
