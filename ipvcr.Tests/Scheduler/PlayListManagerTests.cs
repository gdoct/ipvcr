namespace ipvcr.Tests.Scheduler;

using ipvcr.Logic;
using ipvcr.Logic.Api;
using ipvcr.Logic.Scheduler;
using ipvcr.Logic.Settings;
using Moq;
using System;
using System.IO.Abstractions;
using System.Text;
using System.Threading.Tasks;
public class PlaylistManagerTests
{
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<IFileSystem> _fileSystemMock;

    public PlaylistManagerTests()
    {
        _settingsServiceMock = new Mock<ISettingsService>();
        _fileSystemMock = new Mock<IFileSystem>();
    }

    [Fact]
    public void PlaylistManager_Constructor_ValidSettings()
    {
        var settingsService = new Mock<ISettingsService>();
        var fileSystem = new Mock<IFileSystem>();
        var playlistPath = "valid_playlist.m3u";

        var m3uContent = "#EXTM3U\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 4\" tvg-name=\"4K | RTL 4\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL4.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 4\n" +
                             "http://example.com/stream1\n" +
                             "##### SUPERGROUP #####\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 5\" tvg-name=\"4K | RTL 5\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL5.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 5\n" +
                             "http://example.com/stream2\n";
        var expectedChannels = new List<ChannelInfo>
            {
                new ("1", "4K | RTL 4", "https://logo/images/logos/NEDERLAND-NEW1/RTL4.png", new Uri("http://example.com/stream1"), "NL | 4K NEDERLAND"),
                new ("2", "4K | RTL 5", "https://logo/images/logos/NEDERLAND-NEW1/RTL5.png", new Uri("http://example.com/stream2"), "NL | 4K NEDERLAND")
            };

        var file = new Mock<IFile>();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(m3uContent));
        fileSystem.SetupGet(x => x.File).Returns(file.Object);
        file.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
        file.Setup(x => x.OpenRead(It.IsAny<string>()))
            .Returns(new MockedFileSystemStream(stream, playlistPath, true));

        var s = new SchedulerSettings { DataPath = playlistPath };
        settingsService.SetupGet(s => s.SchedulerSettings).Returns(s);
        settingsService.SetupGet(s => s.PlaylistSettings).Returns(new PlaylistSettings() { M3uPlaylistPath = playlistPath });
        fileSystem.Setup(fs => fs.File.Exists(playlistPath)).Returns(true);

        var playlistManager = new PlaylistManager(settingsService.Object, fileSystem.Object);

        // Assert
        Assert.NotNull(playlistManager);
    }

    [Fact]
    public void PlaylistManager_Constructor_TaskFaults()
    {
        var settingsService = new Mock<ISettingsService>();
        var fileSystem = new Mock<IFileSystem>();
        var playlistPath = "valid_playlist.m3u";

        var m3uContent = "#EXTM3U\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 4\" tvg-name=\"4K | RTL 4\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL4.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 4\n" +
                             "http://example.com/stream1\n" +
                             "##### SUPERGROUP #####\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 5\" tvg-name=\"4K | RTL 5\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL5.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 5\n" +
                             "http://example.com/stream2\n";
        var expectedChannels = new List<ChannelInfo>
            {
                new ("1", "4K | RTL 4", "https://logo/images/logos/NEDERLAND-NEW1/RTL4.png", new Uri("http://example.com/stream1"), "NL | 4K NEDERLAND"),
                new ("2", "4K | RTL 5", "https://logo/images/logos/NEDERLAND-NEW1/RTL5.png", new Uri("http://example.com/stream2"), "NL | 4K NEDERLAND")
            };

        var file = new Mock<IFile>();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(m3uContent));
        fileSystem.SetupGet(x => x.File).Returns(file.Object);
        file.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
        file.Setup(x => x.OpenRead(It.IsAny<string>()))
            .Throws<IOException>();
        //            .Returns(new MockedFileSystemStream(stream, playlistPath, true));

        var s = new SchedulerSettings { DataPath = playlistPath };
        settingsService.SetupGet(s => s.SchedulerSettings).Returns(s);
        settingsService.SetupGet(s => s.PlaylistSettings).Returns(new PlaylistSettings() { M3uPlaylistPath = playlistPath });

        fileSystem.Setup(fs => fs.File.Exists(playlistPath)).Returns(true);

        Assert.Throws<InvalidOperationException>(() => new PlaylistManager(settingsService.Object, fileSystem.Object));
    }

    [Fact]
    public async Task PlaylistManager_LoadFromFileAsync_ValidPath()
    {
        // Arrange
        var settingsService = new Mock<ISettingsService>();
        var fileSystem = new Mock<IFileSystem>();
        var playlistPath = "valid_playlist.m3u";

        var m3uContent = "#EXTM3U\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 4\" tvg-name=\"4K | RTL 4\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL4.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 4\n" +
                             "http://example.com/stream1\n" +
                             "##### SUPERGROUP #####\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 5\" tvg-name=\"4K | RTL 5\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL5.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 5\n" +
                             "http://example.com/stream2\n";

        var file = new Mock<IFile>();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(m3uContent));
        fileSystem.SetupGet(x => x.File).Returns(file.Object);
        file.Setup(x => x.Exists(playlistPath)).Returns(true);
        file.Setup(x => x.OpenRead(playlistPath))
            .Returns(new MockedFileSystemStream(stream, playlistPath, true));

        var s = new SchedulerSettings { DataPath = playlistPath };
        settingsService.SetupGet(s => s.SchedulerSettings).Returns(s);
        settingsService.SetupGet(s => s.PlaylistSettings).Returns(new PlaylistSettings() { M3uPlaylistPath = playlistPath });
        fileSystem.Setup(fs => fs.File.Exists(playlistPath)).Returns(true);

        var playlistManager = new PlaylistManager(settingsService.Object, fileSystem.Object);

        var newPlaylistPath = "new_playlist.m3u";
        var m3uContent2 = "#EXTM3U\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 4\" tvg-name=\"4K | RTL 4\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL4.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 4\n" +
                             "http://example.com/stream1\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 5\" tvg-name=\"4K | RTL 5\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL5.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 5\n" +
                             "http://example.com/stream2\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 7\" tvg-name=\"4K | RTL 7\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL7.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 5\n" +
                             "http://example.com/stream2\n";

        var file2 = new Mock<IFile>();
        var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(m3uContent2));
        fileSystem.SetupGet(x => x.File).Returns(file2.Object);
        file2.Setup(x => x.Exists(newPlaylistPath)).Returns(true);
        file2.Setup(x => x.OpenRead(newPlaylistPath))
            .Returns(new MockedFileSystemStream(stream2, newPlaylistPath, true));

        await playlistManager.LoadFromFileAsync(newPlaylistPath);
        var lst = new List<ChannelInfo>();
        Assert.Equal(3, playlistManager.GetPlaylistItems().Count);
        Assert.Equal(3, playlistManager.ChannelCount);
    }

    [Fact]
    public void PlaylistManager_LoadFromFileAsync_EmptyPath_Throws()
    {
        // Arrange
        var settingsService = new Mock<ISettingsService>();
        var fileSystem = new Mock<IFileSystem>();

        var playlistPath = "valid_playlist.m3u";

        var m3uContent = "#EXTM3U\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 4\" tvg-name=\"4K | RTL 4\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL4.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 4\n" +
                             "http://example.com/stream1\n" +
                             "##### SUPERGROUP #####\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 5\" tvg-name=\"4K | RTL 5\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL5.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 5\n" +
                             "http://example.com/stream2\n";
        var expectedChannels = new List<ChannelInfo>
            {
                new ("1", "4K | RTL 4", "https://logo/images/logos/NEDERLAND-NEW1/RTL4.png", new Uri("http://example.com/stream1"), "NL | 4K NEDERLAND"),
                new ("2", "4K | RTL 5", "https://logo/images/logos/NEDERLAND-NEW1/RTL5.png", new Uri("http://example.com/stream2"), "NL | 4K NEDERLAND")
            };

        var file = new Mock<IFile>();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(m3uContent));
        fileSystem.SetupGet(x => x.File).Returns(file.Object);
        file.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
        file.Setup(x => x.OpenRead(It.IsAny<string>()))
            .Returns(new MockedFileSystemStream(stream, playlistPath, true));

        var s = new SchedulerSettings { DataPath = "playlist.m3u" };
        fileSystem.Setup(fs => fs.File.Exists(s.DataPath)).Returns(true);
        settingsService.SetupGet(s => s.SchedulerSettings).Returns(s);
        settingsService.SetupGet(s => s.PlaylistSettings).Returns(new PlaylistSettings() { M3uPlaylistPath = playlistPath });
        var playlistManager = new PlaylistManager(settingsService.Object, fileSystem.Object);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => playlistManager.LoadFromFileAsync(string.Empty));
    }

    [Fact]
    public void PlaylistManager_LoadFromFileAsync_NotFoundThrows()
    {
        // Arrange
        var settingsService = new Mock<ISettingsService>();
        var fileSystem = new Mock<IFileSystem>();
        var playlistPath = "not_found_playlist.m3u";
        var s = new SchedulerSettings
        {
            DataPath = playlistPath
        };
        settingsService.SetupGet(s => s.SchedulerSettings).Returns(s);
        settingsService.SetupGet(s => s.PlaylistSettings).Returns(new PlaylistSettings() { M3uPlaylistPath = playlistPath });
        fileSystem.Setup(fs => fs.File.Exists(playlistPath)).Returns(false);

        var playlistManager = new PlaylistManager(settingsService.Object, fileSystem.Object);

        // Act & Assert
        Assert.ThrowsAsync<FileNotFoundException>(() => playlistManager.LoadFromFileAsync(playlistPath));
    }

    [Fact]
    public void PlaylistManager_Ctor_EmptyM3uPlaylistPathThrows()
    {
        // Arrange
        var settingsService = new Mock<ISettingsService>();

        var s = new SchedulerSettings
        {
            DataPath = string.Empty
        };
        settingsService.SetupGet(s => s.SchedulerSettings).Returns(s);
        settingsService.SetupGet(s => s.PlaylistSettings).Returns(new PlaylistSettings() { M3uPlaylistPath = "" });
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new PlaylistManager(settingsService.Object, Mock.Of<IFileSystem>()));
    }

    [Fact]
    public void PlaylistManager_Ctor_EmptyFileSystemThrows()
    {
        // Arrange
        var settingsService = new Mock<ISettingsService>();
        var o = new object();
#pragma warning disable CS8604 // Possible null reference argument.
        Assert.Throws<ArgumentNullException>(() => new PlaylistManager(settingsService.Object, o as IFileSystem));
#pragma warning restore CS8604 // Possible null reference argument.
    }

    [Fact]
    public void PlaylistManager_SettingsChanged_SamePlaylistFile()
    {
        var settingsService = new Mock<ISettingsService>();
        var fileSystem = new Mock<IFileSystem>();
        var playlistPath = "valid_playlist.m3u";

        var m3uContent = "#EXTM3U\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 4\" tvg-name=\"4K | RTL 4\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL4.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 4\n" +
                             "http://example.com/stream1\n" +
                             "##### SUPERGROUP #####\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 5\" tvg-name=\"4K | RTL 5\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL5.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 5\n" +
                             "http://example.com/stream2\n";
        var expectedChannels = new List<ChannelInfo>
            {
                new ("1", "4K | RTL 4", "https://logo/images/logos/NEDERLAND-NEW1/RTL4.png", new Uri("http://example.com/stream1"), "NL | 4K NEDERLAND"),
                new ("2", "4K | RTL 5", "https://logo/images/logos/NEDERLAND-NEW1/RTL5.png", new Uri("http://example.com/stream2"), "NL | 4K NEDERLAND")
            };

        var file = new Mock<IFile>();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(m3uContent));
        fileSystem.SetupGet(x => x.File).Returns(file.Object);
        file.Setup(x => x.OpenRead(It.IsAny<string>()))
            .Returns(new MockedFileSystemStream(stream, playlistPath, true));

        var s = new SchedulerSettings { DataPath = playlistPath };
        settingsService.SetupGet(s => s.PlaylistSettings).Returns(new PlaylistSettings() { M3uPlaylistPath = "data" });

        fileSystem.Setup(fs => fs.File.Exists(playlistPath)).Returns(true);

        var playlistManager = new PlaylistManager(settingsService.Object, fileSystem.Object);

        // Act
        settingsService.Raise(x => x.SettingsChanged += null, new SettingsServiceChangedEventArgs(SettingsType.Playlist, new PlaylistSettings()
        {
            M3uPlaylistPath = playlistPath
        }));

        // Assert
        // No exception should be thrown
        fileSystem.VerifyAll();
        settingsService.VerifyAll();
    }

    [Fact]
    public void PlaylistManager_SettingsChanged_ReloadPlaylist()
    {
        var settingsService = new Mock<ISettingsService>();
        var fileSystem = new Mock<IFileSystem>();
        var playlistPath = "valid_playlist.m3u";
        var newplaylistPath = "new_playlist.m3u";
        var m3uContent = "#EXTM3U\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 4\" tvg-name=\"4K | RTL 4\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL4.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 4\n" +
                             "http://example.com/stream1\n" +
                             "##### SUPERGROUP #####\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 5\" tvg-name=\"4K | RTL 5\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL5.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 5\n" +
                             "http://example.com/stream2\n";
        var expectedChannels = new List<ChannelInfo>
            {
                new ("1", "4K | RTL 4", "https://logo/images/logos/NEDERLAND-NEW1/RTL4.png", new Uri("http://example.com/stream1"), "NL | 4K NEDERLAND"),
                new ("2", "4K | RTL 5", "https://logo/images/logos/NEDERLAND-NEW1/RTL5.png", new Uri("http://example.com/stream2"), "NL | 4K NEDERLAND")
            };

        var file = new Mock<IFile>();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(m3uContent));
        var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(m3uContent));
        fileSystem.SetupGet(x => x.File).Returns(file.Object);
        file.Setup(x => x.OpenRead(It.IsAny<string>()))
            .Returns(new MockedFileSystemStream(stream, playlistPath, true));

        // var s = new SchedulerSettings { DataPath = playlistPath };
        // settingsService.SetupGet(s => s.SchedulerSettings).Returns(s);
        settingsService.SetupGet(s => s.PlaylistSettings).Returns(new PlaylistSettings() { M3uPlaylistPath = playlistPath });
        fileSystem.Setup(fs => fs.File.Exists(playlistPath)).Returns(true);

        var playlistManager = new PlaylistManager(settingsService.Object, fileSystem.Object);

        // Act
        var newSettings = new SchedulerSettings { DataPath = newplaylistPath };

        //  fileSystem.SetupGet(x => x.File).Returns(file.Object);
        // file.Setup(x => x.OpenRead(It.IsAny<string>()))
        //     .Returns(new MockedFileSystemStream(stream2, newplaylistPath, true));
        // fileSystem.Setup(fs => fs.File.Exists(newplaylistPath)).Returns(true);

        settingsService.Raise(x => x.SettingsChanged += null, new SettingsServiceChangedEventArgs(SettingsType.Scheduler, newSettings));

        // Assert
        // No exception should be thrown
        fileSystem.VerifyAll();
        settingsService.VerifyAll();
    }
}