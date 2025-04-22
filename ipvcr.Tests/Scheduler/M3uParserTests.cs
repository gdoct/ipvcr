using ipvcr.Logic;
using ipvcr.Logic.Scheduler;
using Moq;
using System.IO.Abstractions;
using System.Text;

namespace ipvcr.Tests.Scheduler
{
    public class M3uParserTests
    {
        [Fact]
        public async Task M3uParser_ParseM3uFile_ValidInput_ReturnsCorrectChannels()
        {
            // Arrange
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

            // Act
            var fs = new Mock<IFileSystem>();
            var file = new Mock<IFile>();
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(m3uContent));
            fs.SetupGet(x => x.File).Returns(file.Object);
            file.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
            file.Setup(x => x.OpenRead(It.IsAny<string>()))
                .Returns(new MockedFileSystemStream(stream, "test.m3u", true));
            var parser = new M3uParser(fs.Object, "test.m3u");
            var result = new List<ChannelInfo>();
            await foreach (var channel in parser.ParsePlaylistAsync())
            {
                result.Add(channel);
            }

            // Assert
            Assert.Equal(expectedChannels.Count, result.Count);
            for (int i = 0; i < expectedChannels.Count; i++)
            {
                Assert.Equal(expectedChannels[i].Name, result[i].Name);
                Assert.Equal(expectedChannels[i].Uri, result[i].Uri);
            }
        }

        [Fact]
        public void M3uParser_Constructor_IncorrectArgs_ThrowsArgumentNullException()
        {
            // Arrange
            string path = "test.m3u";
            object q = new();
            // Act & Assert
#pragma warning disable CS8604 // Possible null reference argument.
            Assert.Throws<ArgumentNullException>(() => new M3uParser(q as IFileSystem, path));
#pragma warning restore CS8604 // Possible null reference argument.
            Assert.Throws<ArgumentNullException>(() => new M3uParser(new FileSystem(), string.Empty));
            Assert.Throws<FileNotFoundException>(() => new M3uParser(new FileSystem(), $"test-{Guid.NewGuid()}.m3u"));
        }

        [Fact]
        public void M3uParser_Constructor_WorksWithEmptyFile()
        {
            var file = Path.GetTempFileName();
            File.WriteAllText(file, string.Empty);
            var parser = new M3uParser(file);
            Assert.NotNull(parser);
        }
    }
}