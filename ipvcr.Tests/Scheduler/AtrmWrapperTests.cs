namespace ipvcr.Tests.Scheduler;

using ipvcr.Logic.Api;
using ipvcr.Logic.Scheduler;
using Moq;

public class AtrmWrapperTests
{
    private readonly Mock<IProcessRunner> _processRunnerMock;
    private readonly Mock<ISettingsService> _settingsManagerMock;

    public AtrmWrapperTests()
    {
        _processRunnerMock = new Mock<IProcessRunner>();
        _settingsManagerMock = new Mock<ISettingsService>();
    }

    [Fact]
    public void Atrm_RemoveTask_ShouldCallAtrmWithCorrectJobId()
    {
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "atrm")).Returns(("atrm", "", 0));
        var atrmWrapper = new AtrmWrapper(_processRunnerMock.Object, _settingsManagerMock.Object);

        var jobId = 12345;
        var expectedCommand = $"atrm {jobId}";

        // Act
        atrmWrapper.CancelTask(jobId);

        // Assert
        _processRunnerMock.Verify(pr => pr.RunProcess("atrm", jobId.ToString()), Times.Once);
    }

    [Fact]
    public void Atrm_RemoveTask_ThrowsIfAtrmFails()
    {
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "atrm")).Returns(("atrm", "", 0));
        var atrmWrapper = new AtrmWrapper(_processRunnerMock.Object, _settingsManagerMock.Object);

        var jobId = 12345;
        var expectedError = "Error occurred";
        _processRunnerMock.Setup(pr => pr.RunProcess("atrm", jobId.ToString()))
            .Returns(("", expectedError, 1));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => atrmWrapper.CancelTask(jobId));
        Assert.Equal($"Failed to delete task with job ID {jobId}: {expectedError}", exception.Message);
    }
}
