using ipvcr.Logic.Api;
using System.Text.RegularExpressions;

namespace ipvcr.Logic.Scheduler;

public class AtqWrapper(IProcessRunner processRunner, ISettingsService settingsService) :
                CommandWrapperBase(processRunner, settingsService, AtqCommand)
{
    private const string AtqCommand = "atq";
    private static readonly Regex _atqJobIdRegex = new(@"^(\d+)\s+", RegexOptions.Compiled);

    private bool TryParseJobId(string atqLine, out int jobId)
    {
        // The output of `atq` is typically in the format:
        // job_id   date_time   queue_name   user
        // We only need the job_id, which is the first column.
        // Example output:
        // 1 2023-10-01 12:00 a user
        // 2 2023-10-02 14:00 b user
        var match = _atqJobIdRegex.Match(atqLine);
        if (match.Success)
        {
            // jobId is guaranteed to be integer here (due to the regex)
            jobId = int.Parse(match.Groups[1].Value);
            return true;
        }
        else
        {
            // set out parameter jobId to 0 if parsing fails
            jobId = 0;
            return false;
        }
    }


    public IEnumerable<int> GetScheduledTasks()
    {
        // The output of `atq` is typically in the format:
        // job_id   date_time   queue_name   user
        // We only need the job_id, which is the first column.
        // Example output:
        // 12345 2023-10-01 12:00 a user
        // 67890 2023-10-02 14:00 b user

        // Run the atq command and capture the output
        // Note: The output may vary based on the system and user permissions.
        // The command may need to be adjusted based on the system's locale and date format.
        var (output, error, exitCode) = base.ExecuteCommand(string.Empty);

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Failed to list tasks: {error}");
        }
        var lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (TryParseJobId(line, out var jobId))
            {
                yield return jobId;
            }
        }
    }
}