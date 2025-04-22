using ipvcr.Logic.Api;
using System.IO.Abstractions;

namespace ipvcr.Logic.Settings;

public class SslSettingsManager : BaseSettingsManager<SslSettings>, ISettingsManager<SslSettings>
{
    const string SETTINGS_FILENAME = "ssl-settings.json";

    public SslSettingsManager(IFileSystem filesystem)
        : base(filesystem, SETTINGS_FILENAME, "/data")
    {
    }
}