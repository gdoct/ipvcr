using ipvcr.Logic.Api;
using System.IO.Abstractions;

namespace ipvcr.Logic.Settings;

public class FfmpegSettingsManager : BaseSettingsManager<FfmpegSettings>, ISettingsManager<FfmpegSettings>
{
    const string SETTINGS_FILENAME = "ffmpeg-settings.json";

    public FfmpegSettingsManager(IFileSystem filesystem)
        : base(filesystem, SETTINGS_FILENAME, "/data")
    {
    }
}