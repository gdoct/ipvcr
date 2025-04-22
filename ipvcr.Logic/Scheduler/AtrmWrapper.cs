using ipvcr.Logic.Api;

namespace ipvcr.Logic.Scheduler;

public class AtrmWrapper(IProcessRunner processRunner, ISettingsService settingsManager) : CommandWrapperBase(processRunner, settingsManager, AtrmCommand)
{
    private const string AtrmCommand = "atrm";

    public void CancelTask(int jobId)
    {
        var (_, error, exitCode) = ProcessRunner.RunProcess(AtrmCommand, jobId.ToString());

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Failed to delete task with job ID {jobId}: {error}");
        }
    }
}