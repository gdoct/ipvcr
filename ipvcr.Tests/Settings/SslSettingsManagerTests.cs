using ipvcr.Logic.Settings;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
#pragma warning disable CS8600, CS8602, CS8603, CS8604, CS8618, CS8625
namespace ipvcr.Tests.Settings
{
    public class SslSettingsManagerTests
    {
        private readonly MockFileSystem _mockFileSystem;
        private const string SettingsFilePath = "/data/ssl-settings.json";

        public SslSettingsManagerTests()
        {
            _mockFileSystem = new MockFileSystem();
            _mockFileSystem.AddDirectory("/data");
        }

        [Fact]
        public void Constructor_NullFileSystem_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SslSettingsManager(null));
        }

        [Fact]
        public void Constructor_LoadsExistingSettings()
        {
            // Arrange
            var expectedSettings = new SslSettings
            {
                CertificatePath = "/custom/path/cert.pfx",
                CertificatePassword = "custom_password",
                UseSsl = false
            };
            _mockFileSystem.AddFile(SettingsFilePath, new MockFileData(JsonSerializer.Serialize(expectedSettings)));

            // Act
            var manager = new SslSettingsManager(_mockFileSystem);

            // Assert
            var settings = manager.Settings;
            Assert.Equal("/custom/path/cert.pfx", settings.CertificatePath);
            Assert.Equal("custom_password", settings.CertificatePassword);
            Assert.False(settings.UseSsl);
        }

        [Fact]
        public void Constructor_CreatesDefaultSettingsIfFileDoesNotExist()
        {
            // Act
            var manager = new SslSettingsManager(_mockFileSystem);

            // Assert
            var settings = manager.Settings;
            Assert.Equal("/data/ssl-certificates/certificate.pfx", settings.CertificatePath);
            Assert.Equal("default_password", settings.CertificatePassword);
            Assert.True(settings.UseSsl);

            // Verify the settings file was created
            Assert.True(_mockFileSystem.FileExists(SettingsFilePath));

            // Verify the content is the serialized default settings
            var fileContent = _mockFileSystem.GetFile(SettingsFilePath).TextContents;
            var deserializedSettings = JsonSerializer.Deserialize<SslSettings>(fileContent);
            Assert.Equal("/data/ssl-certificates/certificate.pfx", deserializedSettings.CertificatePath);
            Assert.Equal("default_password", deserializedSettings.CertificatePassword);
            Assert.True(deserializedSettings.UseSsl);
        }

        [Fact]
        public void Settings_Set_SavesAndUpdatesFile()
        {
            // Arrange
            var manager = new SslSettingsManager(_mockFileSystem);
            var newSettings = new SslSettings
            {
                CertificatePath = "/updated/path/certificate.pfx",
                CertificatePassword = "updated_password",
                UseSsl = false
            };

            bool eventRaised = false;
            SslSettings eventSettings = null;

            manager.SettingsChanged += (sender, args) =>
            {
                eventRaised = true;
                eventSettings = args.NewSettings;
            };

            // Act
            manager.Settings = newSettings;

            // Assert
            Assert.True(eventRaised);
            Assert.Equal("/updated/path/certificate.pfx", eventSettings.CertificatePath);
            Assert.Equal("updated_password", eventSettings.CertificatePassword);
            Assert.False(eventSettings.UseSsl);

            // Verify the settings file was updated
            var fileContent = _mockFileSystem.GetFile(SettingsFilePath).TextContents;
            var deserializedSettings = JsonSerializer.Deserialize<SslSettings>(fileContent);
            Assert.Equal("/updated/path/certificate.pfx", deserializedSettings.CertificatePath);
            Assert.Equal("updated_password", deserializedSettings.CertificatePassword);
            Assert.False(deserializedSettings.UseSsl);
        }

        [Fact]
        public void Settings_Set_NullValue_ThrowsArgumentNullException()
        {
            // Arrange
            var manager = new SslSettingsManager(_mockFileSystem);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => manager.Settings = null);
        }
    }
}
#pragma warning restore CS8600, CS8602, CS8603, CS8604, CS8618, CS8625