using ipvcr.Logic.Settings;

namespace ipvcr.Logic.Api;

public class SettingsChangedEventArgs<T>(T newSettings) : EventArgs
{
    public T NewSettings { get; } = newSettings;
}

public interface ISettingsManager<T>
{

    T Settings { get; set; }
    event EventHandler<SettingsChangedEventArgs<T>>? SettingsChanged;
}

public interface IAdminSettingsManager : ISettingsManager<AdminPasswordSettings>
{
    string GetAdminPassword();
    void UpdateAdminPassword(string newPassword);
}