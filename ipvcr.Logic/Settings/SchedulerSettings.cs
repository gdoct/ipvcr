namespace ipvcr.Logic.Settings;

public class SchedulerSettings
{
    public string MediaPath { get; set; } = "/media";
    public string DataPath { get; set; } = "/data";
    public bool RemoveTaskAfterExecution { get; set; } = true;
}
