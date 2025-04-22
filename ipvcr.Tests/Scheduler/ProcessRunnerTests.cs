using ipvcr.Logic.Scheduler;
using System.ComponentModel;

namespace ipvcr.Tests.Scheduler;

public class ProcessRunnerTests
{
    [Fact]
    public void RunProcess_WhenCalled_ReturnsOutputErrorAndExitCode()
    {
        // Arrange
        var processRunner = new ProcessRunner();
        var fileName = "echo";
        var arguments = string.Empty;
        // Act
        var (output, error, exitCode) = processRunner.RunProcess(fileName, arguments);

        // Assert
        Assert.True(output.Length > 0);
        Assert.Equal("\n", error);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void RunProcess_WhenCalledWithInvalidCommand_ReturnsErrorAndExitCode()
    {
        // Arrange
        var processRunner = new ProcessRunner();
        var fileName = "invalidcommand";
        var arguments = string.Empty;

        // Act
        Assert.Throws<Win32Exception>(() => processRunner.RunProcess(fileName, arguments));
    }

    [Fact]
    public void RunProcess_WhenCalledWithInvalidArguments_ReturnsErrorAndExitCode()
    {
        // Arrange
        var processRunner = new ProcessRunner();
        var fileName = "grep";
        var arguments = "--invalid-argument";
        // Act
        var (output, error, exitCode) = processRunner.RunProcess(fileName, arguments);

        // Assert
        Assert.Equal(Environment.NewLine, output);
        Assert.Contains("grep: unrecognized option", error);

        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public void RunProcess_ShouldTimeout()
    {
        // Arrange
        var processRunner = new ProcessRunner();
        var fileName = "sleep";
        var arguments = "10";

        // Act
        var (output, error, exitCode) = processRunner.RunProcess(fileName, arguments, 1000);

        // Assert
        Assert.Equal(string.Empty, output);
        Assert.Contains("timed out", error);
        Assert.NotEqual(0, exitCode);
    }
}
