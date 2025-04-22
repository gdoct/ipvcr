namespace ipvcr.Logic.Api;

public interface IProcessRunner
{
    (string output, string error, int exitCode) RunProcess(string fileName, string arguments);
    (string output, string error, int exitCode) RunProcess(string fileName, string arguments, int msTimeOut);
}
