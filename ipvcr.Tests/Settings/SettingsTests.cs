using ipvcr.Logic.Settings;
using Moq;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;

namespace ipvcr.Tests.Settings;

public class SettingsManagerTests
{
    private string SettingsFilePath = "/data/settings.json";

    [Fact]
    public void LoadSettings_FileDoesNotExist_ReturnsDefaultSettings()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var settingsManager = CreateSettingsManager(mockFileSystem);

        // Act
        var settings = settingsManager.Settings;

        // Assert
        Assert.NotNull(settings);
        Assert.Equal("/media", settings.MediaPath);
    }

    [Fact]
    public void LoadSettings_FileExistsWithValidJson_ReturnsDeserializedSettings()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var validJson = JsonSerializer.Serialize(new SchedulerSettings { MediaPath = "/tmp/output" });
        mockFileSystem.AddFile(SettingsFilePath, new MockFileData(validJson));
        var settingsManager = CreateSettingsManager(mockFileSystem);

        // Act
        var settings = settingsManager.Settings;

        // Assert
        Assert.NotNull(settings);
        Assert.Equal("/tmp/output", settings.MediaPath);
    }

    [Fact]
    public void LoadSettings_FileExistsWithInvalidJson_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddFile(SettingsFilePath, new MockFileData("Invalid JSON"));
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _ = CreateSettingsManager(mockFileSystem));
    }

    private static SchedulerSettingsManager CreateSettingsManager(MockFileSystem mockFileSystem)
    {
        // Ensure the /data directory exists
        mockFileSystem.AddDirectory("/data");

        var mockFileSystemWrapper = new Mock<IFileSystem>();
        mockFileSystemWrapper.Setup(fs => fs.File).Returns(mockFileSystem.File);
        mockFileSystemWrapper.Setup(fs => fs.Directory).Returns(mockFileSystem.Directory);
        mockFileSystemWrapper.Setup(fs => fs.Path).Returns(mockFileSystem.Path);

        return new SchedulerSettingsManager(mockFileSystemWrapper.Object); // Adjust constructor if dependency injection is needed
    }

    [Fact]
    public void SettingsManager_SettingsChanged_EventIsRaised()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var settingsManager = CreateSettingsManager(mockFileSystem);
        var settingsChanged = false;
        SchedulerSettings? newsettings = null;
        settingsManager.SettingsChanged += (sender, args) => { settingsChanged = true; newsettings = args.NewSettings; };
        mockFileSystem.AddFile(SettingsFilePath, new MockFileData(JsonSerializer.Serialize(new SchedulerSettings())));
        // Act
        var oldsettings = settingsManager.Settings;
        settingsManager.Settings = new SchedulerSettings { MediaPath = "/new/path" };

        // Assert
        Assert.True(settingsChanged);
        Assert.Equal("/new/path", settingsManager.Settings.MediaPath);
        Assert.NotEqual(oldsettings, newsettings);
    }

    [Fact]
    public void SettingsManager_ConstructorThrowsIfFileSystemIsNull()
    {
        // Act & Assert
        var o = new object();
#pragma warning disable CS8604
        Assert.Throws<ArgumentNullException>(() => new SchedulerSettingsManager(o as IFileSystem));
#pragma warning restore CS8604
    }

    [Fact]
    public void SettingsManager_ReturnsEmptyIfFileDoesNotExist()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var settingsManager = CreateSettingsManager(mockFileSystem);

        // Act & Assert
        // Create an expected settings object with default values
        var expected = new SchedulerSettings();

        var res = settingsManager.Settings;
        Assert.Equivalent(expected, res);
    }

    [Fact]
    public void SettingsManager_ConstructorThrowsIfFileSystemThrowsUnautorizedException()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.File.Exists(It.IsAny<string>()))
            .Returns(true);
        // Set up the mock to throw an UnauthorizedAccessException for a specific method
        mockFileSystem
            .Setup(fs => fs.File.ReadAllText(It.IsAny<string>()))
            .Throws(new UnauthorizedAccessException());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new SchedulerSettingsManager(mockFileSystem.Object));
    }

    [Fact]
    public void SettingsManager_ConstructorThrowsIfFileSystemThrowsIOException()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.File.Exists(It.IsAny<string>()))
            .Returns(true);
        // Set up the mock to throw an UnauthorizedAccessException for a specific method
        mockFileSystem
            .Setup(fs => fs.File.ReadAllText(It.IsAny<string>()))
            .Throws(new IOException());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new SchedulerSettingsManager(mockFileSystem.Object));
    }

    [Fact]
    public void SettingsManager_ConstructorThrowsIfInvalidJson()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddFile(SettingsFilePath, new MockFileData("Invalid JSON"));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => CreateSettingsManager(mockFileSystem));
    }

    [Fact]
    public void SettingsManager_ConstructorOkIfEmptyJson()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddFile(SettingsFilePath, new MockFileData(""));

        // Act & Assert
        Assert.NotNull(CreateSettingsManager(mockFileSystem));
    }

    [Fact]
    public void SettingsManager_ConstructorAllowsNullJson()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddFile(SettingsFilePath, new MockFileData("null"));

        // Act & Assert
        Assert.NotNull(CreateSettingsManager(mockFileSystem));
    }

    [Fact]
    public void SettingsManager_Save()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var settings = new SchedulerSettings { MediaPath = "/new/path" };
        mockFileSystem.AddFile(SettingsFilePath, new MockFileData(JsonSerializer.Serialize(settings)));
        var settingsManager = CreateSettingsManager(mockFileSystem);
        settingsManager.SettingsChanged += (sender, args) => { };
        settings.MediaPath = "/other/path";
        // Act
        settingsManager.Settings = settings;

        // Assert
        var savedJson = mockFileSystem.File.ReadAllText(SettingsFilePath);
        var savedSettings = JsonSerializer.Deserialize<SchedulerSettings>(savedJson);
        Assert.Equal(settings.MediaPath, savedSettings?.MediaPath);
    }

    [Fact]
    public void SettingsManager_SaveThrowsIfFileSystemThrowsUnautorizedException()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.File.Exists(It.IsAny<string>()))
            .Returns(true);
        mockFileSystem
           .Setup(fs => fs.File.ReadAllText(It.IsAny<string>()))
           .Returns(JsonSerializer.Serialize(new SchedulerSettings() { MediaPath = "/tmp/output" }));

        // Set up the mock to throw an UnauthorizedAccessException for a specific method
        mockFileSystem
            .Setup(fs => fs.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new UnauthorizedAccessException());

        // Act & Assert
        var sm = new SchedulerSettingsManager(mockFileSystem.Object);
        Assert.Throws<InvalidOperationException>(() => sm.Settings = new SchedulerSettings());
    }

    [Fact]
    public void SettingsManager_SaveThrowsIfFileSystemThrowsIOException()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(fs => fs.File.Exists(It.IsAny<string>()))
            .Returns(true);
        mockFileSystem
           .Setup(fs => fs.File.ReadAllText(It.IsAny<string>()))
           .Returns(JsonSerializer.Serialize(new SchedulerSettings() { MediaPath = "/tmp/output" }));

        // Set up the mock to throw an UnauthorizedAccessException for a specific method
        mockFileSystem
            .Setup(fs => fs.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new IOException());

        // Act & Assert
        var sm = new SchedulerSettingsManager(mockFileSystem.Object);
        Assert.Throws<InvalidOperationException>(() => sm.Settings = new SchedulerSettings());
    }

    [Fact]
    public void SettingsManager_Save_AllowRecreate()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var settings = new SchedulerSettings { MediaPath = "/new/path" };
        mockFileSystem.AddFile(SettingsFilePath, new MockFileData(JsonSerializer.Serialize(settings)));
        var settingsManager = CreateSettingsManager(mockFileSystem);
        settingsManager.SettingsChanged += (sender, args) => { };
        settings.MediaPath = "/other/path";
        mockFileSystem.RemoveFile(SettingsFilePath);
        settingsManager.Settings = settings;

        // Assert
        var savedJson = mockFileSystem.File.ReadAllText(SettingsFilePath);
        var savedSettings = JsonSerializer.Deserialize<SchedulerSettings>(savedJson);
        Assert.Equal(settings.MediaPath, savedSettings?.MediaPath);
    }

    // [Fact]
    // public void SettingsManager_NewSettings_HasAdminPasswordSet()
    // {
    //     // Arrange
    //     var mockFileSystem = new MockFileSystem();
    //     var settings = new SchedulerSettings { MediaPath = "/new/path" };
    //     mockFileSystem.AddFile(SettingsFilePath, new MockFileData(JsonSerializer.Serialize(settings)));
    //     var settingsManager = CreateSettingsManager(mockFileSystem);
    //     var hash = settingsManager.GetAdminPassword();
    //     var expected = SchedulerSettings.DEFAULT_PASSWORD;
    //     Assert.Equal(expected, hash);
    // }

    // [Fact]
    // public void SettingsManager_UpdatePassword()
    // {
    //     // Arrange
    //     var mockFileSystem = new MockFileSystem();
    //     var settings = new SchedulerSettings { MediaPath = "/new/path" };
    //     mockFileSystem.AddFile(SettingsFilePath, new MockFileData(JsonSerializer.Serialize(settings)));
    //     var settingsManager = CreateSettingsManager(mockFileSystem);
    //     var hash = settingsManager.GetAdminPassword();
    //     var expected = SchedulerSettings.DEFAULT_PASSWORD;
    //     Assert.Equal(expected, hash);

    //     settingsManager.UpdateAdminPassword("unittest");
    //     var newhash2 = settingsManager.GetAdminPassword();
    //     Assert.NotEqual(hash, newhash2);
    //     Assert.Equal("unittest", System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(newhash2)));

    //     Assert.Throws<ArgumentException>(() => settingsManager.UpdateAdminPassword(string.Empty));
    // }

    // [Fact]
    // public void Constructor_SetsDefaultUserAndPassword_WhenFileHasEmptyValues()
    // {
    //     // Arrange
    //     var testJson = """
    //     {
    //         "AdminUsername": "",
    //         "AdminPassword": ""
    //     }
    //     """;
    //     var mockFileSystem = new MockFileSystem();
    //     var filePath = "/data/settings.json";
    //     mockFileSystem.AddFile(filePath, new MockFileData(testJson));

    //     // Act
    //     var manager = new SchedulerSettingsManager(mockFileSystem);

    //     // Assert
    //     Assert.Equal(SchedulerSettings.DEFAULT_USERNAME, manager.Settings.AdminUsername);
    //     Assert.Equal(SchedulerSettings.DEFAULT_PASSWORD, manager.GetAdminPassword());
    // }

    // [Fact]
    // public void SchedulerSettings_DefaultPasswordValueShouldBeCorrect()
    // {
    //     // the default value for the AdminPasssword field is "ipvcr" hashed
    //     // so if the hashing algorithm changes, this test fails until the default is updated
    //     var settings = new SchedulerSettings();
    //     var tokenManager = new TokenManager();
    //     var hash = tokenManager.CreateHash("ipvcr");
    //     var actual = settings.AdminPassword;
    //     Assert.Equal(hash, actual);
    // }
}