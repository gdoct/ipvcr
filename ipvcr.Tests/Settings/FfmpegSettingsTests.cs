using ipvcr.Logic.Settings;

namespace ipvcr.Tests.Settings
{
    public class FfmpegSettingsTests
    {
        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Act
            var settings = new FfmpegSettings();

            // Assert
            Assert.Equal("mp4", settings.FileType);
            Assert.Equal("", settings.Codec);
            Assert.Equal("", settings.AudioCodec);
            Assert.Equal("", settings.VideoBitrate);
            Assert.Equal("", settings.AudioBitrate);
            Assert.Equal("", settings.Resolution);
            Assert.Equal("", settings.FrameRate);
            Assert.Equal("", settings.AspectRatio);
            Assert.Equal("mp4", settings.OutputFormat);
        }

        [Fact]
        public void Properties_CanBeModified()
        {
            // Arrange
            var settings = new FfmpegSettings
            {
                // Act
                FileType = "mkv",
                Codec = "libvpx",
                AudioCodec = "libvorbis",
                VideoBitrate = "2000k",
                AudioBitrate = "192k",
                Resolution = "1920x1080",
                FrameRate = "60",
                AspectRatio = "21:9",
                OutputFormat = "webm"
            };

            // Assert
            Assert.Equal("mkv", settings.FileType);
            Assert.Equal("libvpx", settings.Codec);
            Assert.Equal("libvorbis", settings.AudioCodec);
            Assert.Equal("2000k", settings.VideoBitrate);
            Assert.Equal("192k", settings.AudioBitrate);
            Assert.Equal("1920x1080", settings.Resolution);
            Assert.Equal("60", settings.FrameRate);
            Assert.Equal("21:9", settings.AspectRatio);
            Assert.Equal("webm", settings.OutputFormat);
        }
    }
}