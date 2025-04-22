using ipvcr.Logic.Api;
using System.IO.Abstractions;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ipvcr.Logic.Scheduler;

public partial class AtWrapper(IFileSystem fileSystem, IProcessRunner processRunner, ISettingsService settingsService) :
    CommandWrapperBase(processRunner, settingsService, AT_COMMAND)
{
    private const string AT_DATE_FORMAT = "HH:mm MM/dd/yyyy";
    private const string AT_COMMAND = "at";
    private readonly TaskScriptManager _taskScriptManager = new TaskScriptManager(fileSystem, settingsService);
    public int ScheduleTask(ScheduledTask task)
    {
        Environment.SetEnvironmentVariable("TASK_ID", task.Id.ToString());
        var taskjson = JsonSerializer.Serialize(task);
        //Environment.SetEnvironmentVariable("TASK_DEFINITION", taskjson);
        string startTimeFormatted = task.StartTime.ToLocalTime().ToString(AT_DATE_FORMAT);
        string atCommand = $"echo \"{GetScriptFilename(task)}\" | at {startTimeFormatted}";
        var (output, error, exitCode) = base.ExecuteShellCommand(atCommand);
        //var expected = "warning: commands will be executed using /bin/sh\njob 66 at Thu Apr 17 14:54:00 2025\n\n";
        var lines = error.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length > 0)
        {
            // The first line is the warning message, the second line is the job ID
            foreach (var line in lines)
            {
                var jobIdRegex = new Regex(@"job (\d+) at", RegexOptions.Compiled);
                var match = jobIdRegex.Match(line);
                if (match.Success)
                {
                    string jobIdString = match.Groups[1].Value;
                    return int.Parse(jobIdString);
                }
            }
        }
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Failed to schedule task: {error}");
        }
        if (int.TryParse(output, out int jobId))
        {
            return jobId;
        }
        else
        {
            throw new InvalidOperationException($"Failed to parse job ID from output: {output}");
        }
    }

    private string ExtractTaskId(string scriptContent)
    {
        // example script content:
        // #!/bin/sh
        // # atrun uid=1002 gid=1002
        // ..snip
        // TASK_ID=430e5177-2442-4edb-8176-1d60ce5b5dda; export TASK_ID
        // ..snip
        // /data/tasks/430e5177-2442-4edb-8176-1d60ce5b5dda.sh
        var lines = scriptContent.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.StartsWith("TASK_ID="))
            {
                var match = Regex.Match(line, @"TASK_ID=([a-f0-9\-]+)", RegexOptions.Compiled);
                if (match.Success)
                {
                    var taskId = match.Groups[1].Value;
                    return taskId;
                }
            }
        }
        throw new InvalidOperationException("Task ID not found in output.");
    }

    public (int id, ScheduledTask task) GetTaskDetails(int jobId)
    {
        var (output, error, exitCode) = base.ExecuteCommand($"-c {jobId}");

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Failed to get job details for job {jobId}: {error}");
        }

        if (output.Length == 0)
        {
            throw new InvalidOperationException($"No output from at -c for job {jobId}");
        }
        var taskguid = ExtractTaskId(output);
        if (!Guid.TryParse(taskguid, out var taskId))
        {
            throw new InvalidOperationException($"Failed to parse task ID from output: {output}");
        }
        var taskDefinition = _taskScriptManager.ExtractJsonFromTask(taskId, "TASK_DEFINITION");
        var innerTask = _taskScriptManager.ExtractJsonFromTask(taskId, "TASK_INNER_DEFINITION");
        try
        {
            // Deserialize the task definition
            var task = JsonSerializer.Deserialize<ScheduledTask>(taskDefinition);

            if (task != null)
            {
                // Set the task ID and inner task definition
                task.InnerScheduledTask = innerTask;
                // Return the deserialized task
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
                return (jobId, task);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
            }
        }
        catch (JsonException e)
        {
            throw new InvalidOperationException($"Failed to deserialize task for job {jobId} : {e.ToString()}");
        }
        throw new InvalidOperationException($"Failed to parse task definition for job {jobId}");
    }

}