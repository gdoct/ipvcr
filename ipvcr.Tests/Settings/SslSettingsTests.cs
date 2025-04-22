using ipvcr.Logic.Settings;

namespace ipvcr.Tests.Settings
{
    public class SslSettingsTests
    {
        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Act
            var settings = new SslSettings();

            // Assert
            Assert.Equal("/data/ssl-certificates/certificate.pfx", settings.CertificatePath);
            Assert.Equal("default_password", settings.CertificatePassword);
            Assert.True(settings.UseSsl);
        }

        [Fact]
        public void Properties_CanBeModified()
        {
            // Arrange
            var settings = new SslSettings();

            // Act
            settings.CertificatePath = "/custom/path/cert.pfx";
            settings.CertificatePassword = "custom_password";
            settings.UseSsl = false;

            // Assert
            Assert.Equal("/custom/path/cert.pfx", settings.CertificatePath);
            Assert.Equal("custom_password", settings.CertificatePassword);
            Assert.False(settings.UseSsl);
        }
    }
}