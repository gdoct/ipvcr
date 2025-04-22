using ipvcr.Logic.Settings;

namespace ipvcr.Tests.Settings
{
    public class PlaylistSettingsTests
    {
        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Act
            var settings = new PlaylistSettings();

            // Assert
            Assert.Equal("/data/m3u-playlist.m3u", settings.M3uPlaylistPath);
            Assert.Equal(24, settings.PlaylistAutoUpdateInterval);
            Assert.False(settings.AutoReloadPlaylist);
            Assert.True(settings.FilterEmptyGroups);
        }

        [Fact]
        public void Properties_CanBeModified()
        {
            // Arrange
            var settings = new PlaylistSettings
            {
                // Act
                M3uPlaylistPath = "/custom/path/playlist.m3u",
                PlaylistAutoUpdateInterval = 12,
                AutoReloadPlaylist = true,
                FilterEmptyGroups = false
            };

            // Assert
            Assert.Equal("/custom/path/playlist.m3u", settings.M3uPlaylistPath);
            Assert.Equal(12, settings.PlaylistAutoUpdateInterval);
            Assert.True(settings.AutoReloadPlaylist);
            Assert.False(settings.FilterEmptyGroups);
        }
    }
}