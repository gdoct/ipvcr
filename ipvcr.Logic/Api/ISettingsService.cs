using ipvcr.Logic.Settings;

namespace ipvcr.Logic.Api;

/// <summary>
/// A facade service that provides centralized access to all settings in the application.
/// </summary>
public interface ISettingsService
{
    // Properties for direct settings access with auto-save functionality
    SchedulerSettings SchedulerSettings { get; set; }
    PlaylistSettings PlaylistSettings { get; set; }
    SslSettings SslSettings { get; set; }
    FfmpegSettings FfmpegSettings { get; set; }
    AdminPasswordSettings AdminPasswordSettings { get; set; }
    // Admin password management
    bool ValidateAdminPassword(string passwordhash);
    void UpdateAdminPassword(string newPassword);
    void ResetFactoryDefaults();
    // Settings changed event
    event EventHandler<SettingsServiceChangedEventArgs>? SettingsChanged;
}
