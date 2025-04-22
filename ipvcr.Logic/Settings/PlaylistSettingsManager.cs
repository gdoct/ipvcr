using ipvcr.Logic.Api;
using System.IO.Abstractions;

namespace ipvcr.Logic.Settings;

public class PlaylistSettingsManager : BaseSettingsManager<PlaylistSettings>, ISettingsManager<PlaylistSettings>
{
    const string SETTINGS_FILENAME = "playlist-settings.json";

    public PlaylistSettingsManager(IFileSystem filesystem)
        : base(filesystem, SETTINGS_FILENAME, "/data")
    {
    }
}