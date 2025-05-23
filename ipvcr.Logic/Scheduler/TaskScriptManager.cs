using ipvcr.Logic.Api;
using System.IO.Abstractions;
using System.Text.Json;

namespace ipvcr.Logic.Scheduler;

public class TaskScriptManager(IFileSystem fileSystem, ISettingsService settingsService) : ITaskScriptManager
{
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly ISettingsService _settingsService = settingsService;

    private string ScriptPath => Path.Combine(_settingsService.SchedulerSettings.DataPath, "tasks");
    private string ScriptPathFailed => Path.Combine(_settingsService.SchedulerSettings.DataPath, "tasks", "failed");
    public string TaskScriptPath(Guid taskId) => Path.Combine(ScriptPath, $"{taskId}.sh");

    public void WriteTaskScript(ScheduledTask task, bool removeAfterCompletion)
    {
        string scriptPath = TaskScriptPath(task.Id);
        string scriptContent = GenerateTaskScript(task, removeAfterCompletion);
        _fileSystem.File.WriteAllText(scriptPath, scriptContent);
        _fileSystem.File.SetAttributes(scriptPath, FileAttributes.Normal);
    }

    public string ReadTaskScript(Guid id)
    {
        string scriptPath = TaskScriptPath(id);
        if (_fileSystem.File.Exists(scriptPath))
        {
            return _fileSystem.File.ReadAllText(scriptPath);
        }
        throw new FileNotFoundException($"Task script not found for ID: {id}");
    }

    public string ExtractJsonFromTask(Guid taskId, string definition)
    {
        var taskscript = ReadTaskScript(taskId);
        var lines = taskscript.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.StartsWith($"export {definition}="))
            {
                var json = line.Substring($"export {definition}=".Length).Trim('\"');
                return json;
            }
        }
        throw new InvalidOperationException("Task definition not found in task script.");
    }

    public void MoveScriptToFailed(Guid id)
    {
        string scriptPath = TaskScriptPath(id);
        string failedPath = Path.Combine(ScriptPathFailed, $"{id}.sh");
        if (_fileSystem.File.Exists(scriptPath))
        {
            if (!_fileSystem.Directory.Exists(ScriptPathFailed))
            {
                _fileSystem.Directory.CreateDirectory(ScriptPathFailed);
            }
            if (_fileSystem.File.Exists(failedPath))
            {
                _fileSystem.File.Delete(failedPath);
            }
            _fileSystem.File.Move(scriptPath, failedPath);
        }
    }
    public void RemoveTaskScript(Guid id)
    {
        string scriptPath = TaskScriptPath(id);
        if (_fileSystem.File.Exists(scriptPath))
        {
            _fileSystem.File.Delete(scriptPath);
        }
    }

    private static string GenerateTaskScript(ScheduledTask task, bool removeAfterCompletion)
    {
        var copyWithoutInner = new ScheduledTask(
            id: task.Id,
            name: task.Name,
            command: task.Command,
            startTime: task.StartTime,
            innerScheduledTask: string.Empty
        );
        var json = JsonSerializer.Serialize(copyWithoutInner);
        if (removeAfterCompletion)
        {
            return @$"#!/bin/bash
# this script is generated by ipvcr

export TASK_JOB_ID={task.Id}
export TASK_DEFINITION={json}
export TASK_INNER_DEFINITION='{task.InnerScheduledTask}'

{task.Command}

rm -f ""{task.Id}.sh""";
        }
        else
        {
            return @$"#!/bin/bash
# this script is generated by ipvcr

export TASK_JOB_ID={task.Id}
export TASK_DEFINITION={json}
export TASK_INNER_DEFINITION={task.InnerScheduledTask}

{task.Command}

mkdir -p completed
mv ""{task.Id}.sh"" completed/";
        }
    }

}
