using ipvcr.Logic.Api;

namespace ipvcr.Logic.Scheduler;

public class RecordingSchedulingContext(ITaskScheduler taskScheduler, ISettingsService settingsService) : IRecordingSchedulingContext
{
    private readonly ISettingsService SettingsService = settingsService;
    private ITaskScheduler Scheduler { get; init; } = taskScheduler;

    public IEnumerable<ScheduledRecording> Recordings => Scheduler
                            .FetchScheduledTasks()
                            .Select(ScheduledRecording.FromScheduledTask);

    public void AddRecording(ScheduledRecording recording) => Scheduler.ScheduleTask(recording.ToScheduledTask(SettingsService.FfmpegSettings));

    public void RemoveRecording(Guid recordingId) => Scheduler.CancelTask(recordingId);

    public string GetTaskDefinition(Guid id)
    {
        return Scheduler.GetTaskDefinition(id);
    }

    public void UpdateTaskDefinition(Guid taskId, string newDefinition)
    {
        Scheduler.UpdateTaskDefinition(taskId, newDefinition);
    }
    
    public Settings.FfmpegSettings GetDefaultFfmpegSettings()
    {
        return SettingsService.FfmpegSettings;
    }
}
