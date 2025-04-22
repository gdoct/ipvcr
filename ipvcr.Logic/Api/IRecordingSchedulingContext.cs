namespace ipvcr.Logic.Api;

public interface IRecordingSchedulingContext
{
    IEnumerable<ScheduledRecording> Recordings { get; }

    void AddRecording(ScheduledRecording recording);
    string GetTaskDefinition(Guid id);
    void UpdateTaskDefinition(Guid taskId, string newDefinition);
    void RemoveRecording(Guid recordingId);
    Settings.FfmpegSettings GetDefaultFfmpegSettings();
}
