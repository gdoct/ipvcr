using ipvcr.Logic.Api;
using System.IO.Abstractions;

namespace ipvcr.Logic.Scheduler;

public class AtScheduler(IFileSystem fileSystem, IProcessRunner processRunner, ISettingsService settingsService) : ITaskScheduler
{
    private readonly AtWrapper _atWrapper = new AtWrapper(fileSystem, processRunner, settingsService);
    private readonly AtqWrapper _atqWrapper = new AtqWrapper(processRunner, settingsService);
    private readonly AtrmWrapper _atrmWrapper = new AtrmWrapper(processRunner, settingsService);
    private readonly TaskScriptManager _taskScriptManager = new TaskScriptManager(fileSystem, settingsService);

    private IEnumerable<(int JobId, ScheduledTask Task)> Tasks =>
        _atqWrapper.GetScheduledTasks()
        .Select(jobId =>
        {
            var task = _atWrapper.GetTaskDetails(jobId);
            return task;
        });

    public void CancelTask(Guid taskId)
    {
        var matchingtask = Tasks.FirstOrDefault(t => t.Task.Id == taskId);
        if (matchingtask == default)
        {
            throw new InvalidOperationException($"Task with ID {taskId} not found.");
        }

        _atrmWrapper.CancelTask(matchingtask.JobId);
        _taskScriptManager.MoveScriptToFailed(taskId);
    }

    public IEnumerable<ScheduledTask> FetchScheduledTasks() =>
        Tasks.Select(t => t.Task);

    public string GetTaskDefinition(Guid id)
    {
        var task = Tasks.FirstOrDefault(t => t.Task.Id == id);
        if (task == default)
        {
            throw new InvalidOperationException($"Task with ID {id} not found.");
        }

        return _taskScriptManager.ReadTaskScript(id);
    }

    public void ScheduleTask(ScheduledTask task)
    {
        _atWrapper.ScheduleTask(task);
        _taskScriptManager.WriteTaskScript(task, false);
    }

    public void UpdateTaskDefinition(Guid taskId, string newDefinition)
    {
        var task = Tasks.FirstOrDefault(t => t.Task.Id == taskId);
        if (task == default)
        {
            throw new InvalidOperationException($"Task with ID {taskId} not found.");
        }

        _taskScriptManager.WriteTaskScript(task.Task, false);
    }
}