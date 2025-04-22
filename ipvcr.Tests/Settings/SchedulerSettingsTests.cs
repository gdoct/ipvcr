using ipvcr.Logic.Settings;

namespace ipvcr.Tests.Settings
{
    public class SchedulerSettingsTests
    {
        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            // Act
            var settings = new SchedulerSettings();

            // Assert
            Assert.Equal("/media", settings.MediaPath);
            Assert.Equal("/data", settings.DataPath);
            Assert.True(settings.RemoveTaskAfterExecution);
        }

        [Fact]
        public void Properties_CanBeModified()
        {
            // Arrange
            var settings = new SchedulerSettings
            {
                // Act
                MediaPath = "/custom/media",
                DataPath = "/custom/data",
                RemoveTaskAfterExecution = false
            };

            // Assert
            Assert.Equal("/custom/media", settings.MediaPath);
            Assert.Equal("/custom/data", settings.DataPath);
            Assert.False(settings.RemoveTaskAfterExecution);
        }
    }
}