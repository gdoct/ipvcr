using ipvcr.Logic.Api;
using System.IO.Abstractions;

namespace ipvcr.Logic.Settings;

public class SchedulerSettingsManager : BaseSettingsManager<SchedulerSettings>, ISettingsManager<SchedulerSettings>
{
    const string SETTINGS_FILENAME = "settings.json";

    public SchedulerSettingsManager(IFileSystem filesystem)
        : base(filesystem, SETTINGS_FILENAME, "/data")
    {
    }
}
