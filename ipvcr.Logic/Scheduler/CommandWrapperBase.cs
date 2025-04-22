using ipvcr.Logic.Api;

namespace ipvcr.Logic.Scheduler
{
    public abstract class CommandWrapperBase
    {
        protected readonly IProcessRunner ProcessRunner;
        protected readonly ISettingsService SettingsService;

        protected CommandWrapperBase(IProcessRunner processRunner, ISettingsService settingsService, string command)
        {
            ProcessRunner = processRunner;
            SettingsService = settingsService;
            Command = command;
            EnsureCommandIsInstalled(command);
        }

        protected string Command { get; init; }

        public virtual (string output, string error, int exitCode) ExecuteCommand(string arguments) =>
            ProcessRunner.RunProcess(Command, arguments);

        public virtual (string output, string error, int exitCode) ExecuteShellCommand(string shellCommand) =>
            ProcessRunner.RunProcess("/bin/bash", $"-c \"{shellCommand}\"");

        protected void EnsureCommandIsInstalled(string command)
        {
            var (output, _, _) = ProcessRunner.RunProcess("which", command);

            if (string.IsNullOrWhiteSpace(output))
            {
                throw new MissingDependencyException(command);
            }
        }

        protected string GetScriptFilename(ScheduledTask task) =>
            Path.Combine(SettingsService.SchedulerSettings.DataPath, "tasks", $"{task.Id}.sh");
    }
}