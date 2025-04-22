using ipvcr.Logic.Scheduler;

namespace ipvcr.Logic.Api;

public interface ITaskScheduler
{
    void ScheduleTask(ScheduledTask task);
    IEnumerable<ScheduledTask> FetchScheduledTasks();
    void CancelTask(Guid taskId);
    string GetTaskDefinition(Guid id);
    void UpdateTaskDefinition(Guid taskId, string newDefinition);
}