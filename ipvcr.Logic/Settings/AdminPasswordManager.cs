using ipvcr.Logic.Api;
using System.IO.Abstractions;

namespace ipvcr.Logic.Settings;

public class AdminPasswordManager : BaseSettingsManager<AdminPasswordSettings>, IAdminSettingsManager, ISettingsManager<AdminPasswordSettings>
{
    private readonly ITokenManager _tokenManager;
    private const string DEFAULT_PASSWORD = "default_password";
    private const string DEFAULT_USERNAME = "admin";
    const string SETTINGS_FILENAME = "adminpassword.json";

    public AdminPasswordManager(IFileSystem fileSystem, ITokenManager tokenManager) : base(fileSystem, SETTINGS_FILENAME, "/data")
    {
        if (tokenManager == null)
            throw new ArgumentNullException(nameof(tokenManager));

        _tokenManager = tokenManager;
    }

    public string AdminUsername { get; set; } = DEFAULT_USERNAME;
    public string AdminPassword { get; set; } = DEFAULT_PASSWORD;
    public override AdminPasswordSettings Settings
    {
        get
        {
            // create a copy of the current adminpasswordsettings and set admin password to empty
            // this is to prevent the password from being exposed when returning the settings
            var settings = new AdminPasswordSettings
            {
                AdminUsername = _settings.AdminUsername,
                AdminPassword = string.Empty
            };
            return settings;

        }
        set
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Settings cannot be null.");
            }
            if (string.IsNullOrWhiteSpace(value.AdminUsername))
            {
                throw new ArgumentException("Admin username cannot be null or empty.", nameof(value.AdminUsername));
            }
            var current = _settings;
            if (current.AdminUsername != value.AdminUsername)
            {
                _settings.AdminUsername = value.AdminUsername;
                SaveSettings(_settings);
                // Make sure to raise the event when settings are changed
                RaiseSettingsChanged(_settings);
            }
        }
    }
    protected override AdminPasswordSettings LoadSettings()
    {
        var settings = base.LoadSettings();

        if (string.IsNullOrWhiteSpace(settings.AdminUsername))
        {
            settings.AdminUsername = DEFAULT_USERNAME;
        }
        if (string.IsNullOrWhiteSpace(settings.AdminPassword))
        {
            settings.AdminPassword = DEFAULT_PASSWORD;
        }

        return settings;
    }

    public string GetAdminPassword()
    {
        return Settings.AdminPassword;
    }

    public bool ValidateAdminPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be null or empty.", nameof(password));
        }
        var passwordHash = _tokenManager.CreateHash(password);
        var currentpasswordhash = _settings.AdminPassword;
        return passwordHash == currentpasswordhash;
    }

    public void UpdateAdminPassword(string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw new ArgumentException("New password cannot be null or empty.", nameof(newPassword));
        }

        _settings.AdminPassword = _tokenManager.CreateHash(newPassword);
        SaveSettings(_settings);
    }

    public void SetDefaultAdminPassword()
    {
        _settings.AdminPassword = DEFAULT_PASSWORD;
        SaveSettings(_settings);
    }
}
