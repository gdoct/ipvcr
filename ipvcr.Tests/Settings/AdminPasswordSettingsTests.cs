using ipvcr.Logic.Settings;

namespace ipvcr.Tests.Settings
{
    public class AdminPasswordSettingsTests
    {
        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Act
            var settings = new AdminPasswordSettings();

            // Assert
            Assert.Equal("admin", settings.AdminUsername);
            Assert.Equal("MDAwMDAwMDAwMDBpcHZjcg==", settings.AdminPassword);
        }

        [Fact]
        public void Properties_CanBeModified()
        {
            // Arrange
            var settings = new AdminPasswordSettings
            {
                // Act
                AdminUsername = "customadmin",
                AdminPassword = "custompassword"
            };

            // Assert
            Assert.Equal("customadmin", settings.AdminUsername);
            Assert.Equal("custompassword", settings.AdminPassword);
        }
    }
}