using ipvcr.Logic.Settings;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
#pragma warning disable CS8600, CS8602, CS8603, CS8604, CS8618, CS8625
namespace ipvcr.Tests.Settings
{
    public class PlaylistSettingsManagerTests
    {
        private readonly MockFileSystem _mockFileSystem;
        private const string SettingsFilePath = "/data/playlist-settings.json";

        public PlaylistSettingsManagerTests()
        {
            _mockFileSystem = new MockFileSystem();
            _mockFileSystem.AddDirectory("/data");
        }

        [Fact]
        public void Constructor_NullFileSystem_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PlaylistSettingsManager(null));
        }

        [Fact]
        public void Constructor_LoadsExistingSettings()
        {
            // Arrange
            var expectedSettings = new PlaylistSettings
            {
                M3uPlaylistPath = "/custom/path.m3u",
                PlaylistAutoUpdateInterval = 48,
                AutoReloadPlaylist = true
            };
            _mockFileSystem.AddFile(SettingsFilePath, new MockFileData(JsonSerializer.Serialize(expectedSettings)));

            // Act
            var manager = new PlaylistSettingsManager(_mockFileSystem);

            // Assert
            var settings = manager.Settings;
            Assert.Equal("/custom/path.m3u", settings.M3uPlaylistPath);
            Assert.Equal(48, settings.PlaylistAutoUpdateInterval);
            Assert.True(settings.AutoReloadPlaylist);
        }

        [Fact]
        public void Constructor_CreatesDefaultSettingsIfFileDoesNotExist()
        {
            // Act
            var manager = new PlaylistSettingsManager(_mockFileSystem);

            // Assert
            var settings = manager.Settings;
            Assert.Equal("/data/m3u-playlist.m3u", settings.M3uPlaylistPath);
            Assert.Equal(24, settings.PlaylistAutoUpdateInterval);
            Assert.False(settings.AutoReloadPlaylist);

            // Verify the settings file was created
            Assert.True(_mockFileSystem.FileExists(SettingsFilePath));

            // Verify the content is the serialized default settings
            var fileContent = _mockFileSystem.GetFile(SettingsFilePath).TextContents;
            var deserializedSettings = JsonSerializer.Deserialize<PlaylistSettings>(fileContent);
            Assert.Equal("/data/m3u-playlist.m3u", deserializedSettings.M3uPlaylistPath);
            Assert.Equal(24, deserializedSettings.PlaylistAutoUpdateInterval);
        }

        [Fact]
        public void Settings_Set_SavesAndUpdatesFile()
        {
            // Arrange
            var manager = new PlaylistSettingsManager(_mockFileSystem);
            var newSettings = new PlaylistSettings
            {
                M3uPlaylistPath = "/updated/path.m3u",
                PlaylistAutoUpdateInterval = 6,
                AutoReloadPlaylist = true,
                FilterEmptyGroups = false
            };

            bool eventRaised = false;
            PlaylistSettings eventSettings = null;

            manager.SettingsChanged += (sender, args) =>
            {
                eventRaised = true;
                eventSettings = args.NewSettings;
            };

            // Act
            manager.Settings = newSettings;

            // Assert
            Assert.True(eventRaised);
            Assert.Equal("/updated/path.m3u", eventSettings.M3uPlaylistPath);
            Assert.Equal(6, eventSettings.PlaylistAutoUpdateInterval);
            Assert.True(eventSettings.AutoReloadPlaylist);
            Assert.False(eventSettings.FilterEmptyGroups);

            // Verify the settings file was updated
            var fileContent = _mockFileSystem.GetFile(SettingsFilePath).TextContents;
            var deserializedSettings = JsonSerializer.Deserialize<PlaylistSettings>(fileContent);
            Assert.Equal("/updated/path.m3u", deserializedSettings.M3uPlaylistPath);
            Assert.Equal(6, deserializedSettings.PlaylistAutoUpdateInterval);
            Assert.True(deserializedSettings.AutoReloadPlaylist);
            Assert.False(deserializedSettings.FilterEmptyGroups);
        }

        [Fact]
        public void Settings_Set_NullValue_ThrowsArgumentNullException()
        {
            // Arrange
            var manager = new PlaylistSettingsManager(_mockFileSystem);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => manager.Settings = null);
        }
    }
}
#pragma warning restore CS8600, CS8602, CS8603, CS8604, CS8618, CS8625
