using ipvcr.Logic.Api;
using System.Text;

namespace ipvcr.Logic.Scheduler;

public class ProcessRunner : IProcessRunner
{
    public (string output, string error, int exitCode) RunProcess(string fileName,
        string arguments) => RunProcess(fileName, arguments, 10000);

    // eg         var (_, error, exitCode) = _processRunner.RunProcess("/bin/bash", $"-c \"{command}\"");

    public (string output, string error, int exitCode) RunProcess(string fileName, string arguments, int msTimeOut = 10000)
    {

        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, args) => outputBuilder.AppendLine(args.Data);
        process.ErrorDataReceived += (sender, args) => errorBuilder.AppendLine(args.Data);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit(msTimeOut))
        {
            process.Kill();
            return (string.Empty, "Process timed out", -1);
        }

        process.WaitForExit(); // Ensure the process has fully exited

        return (outputBuilder.ToString(), errorBuilder.ToString(), process.ExitCode);
    }

}
