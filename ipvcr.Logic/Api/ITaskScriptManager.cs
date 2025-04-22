using ipvcr.Logic.Scheduler;

namespace ipvcr.Logic.Api;

public interface ITaskScriptManager
{
    string ReadTaskScript(Guid id);
    void WriteTaskScript(ScheduledTask task, bool removeAfterCompletion);

    void MoveScriptToFailed(Guid id);
    void RemoveTaskScript(Guid id);
    string TaskScriptPath(Guid taskId);
    string ExtractJsonFromTask(Guid taskId, string definition);
}
