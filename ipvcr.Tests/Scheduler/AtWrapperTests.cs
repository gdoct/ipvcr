namespace ipvcr.Tests.Scheduler;

using ipvcr.Logic.Api;
using ipvcr.Logic.Scheduler;
using ipvcr.Logic.Settings;
using Moq;
using System.IO.Abstractions;
using System.Text.Json;

public class AtWrapperTests
{
    private readonly Mock<IFileSystem> _filesystemMock;
    private readonly Mock<IProcessRunner> _processRunnerMock;
    private readonly Mock<ISettingsService> _settingsServiceMock;

    public AtWrapperTests()
    {
        _filesystemMock = new Mock<IFileSystem>();
        _processRunnerMock = new Mock<IProcessRunner>();
        _settingsServiceMock = new Mock<ISettingsService>();

    }

    [Fact]
    public void AtWrapper_ScheduleTaskThrowsIfAtReturnsErrorCode()
    {
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        _settingsServiceMock.Setup(sm => sm.SchedulerSettings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var atWrapper = new AtWrapper(_filesystemMock.Object, _processRunnerMock.Object, _settingsServiceMock.Object);

        var task = new ScheduledTask(Guid.NewGuid(), "Test Task", "echo Hello World", DateTimeOffset.UtcNow.AddMinutes(10), "{}");

        var expectedError = "Error occurred";
        _processRunnerMock.Setup(pr => pr.RunProcess(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(("", expectedError, 1));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => atWrapper.ScheduleTask(task));
        Assert.Equal($"Failed to schedule task: {expectedError}", exception.Message);
    }

    [Fact]
    public void AtWrapper_ScheduleTask_ReturnsJobId()
    {
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        _settingsServiceMock.Setup(sm => sm.SchedulerSettings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var atWrapper = new AtWrapper(_filesystemMock.Object, _processRunnerMock.Object, _settingsServiceMock.Object);

        var task = new ScheduledTask(Guid.NewGuid(), "Test Task", "echo Hello World", DateTimeOffset.UtcNow.AddMinutes(10), "{}");

        var expectedJobId = 123;
        _processRunnerMock.Setup(pr => pr.RunProcess(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(("123", "job " + expectedJobId.ToString() + " at 12:34", 0));

        // Act
        var jobId = atWrapper.ScheduleTask(task);

        // Assert
        Assert.Equal(expectedJobId, jobId);
    }

    [Fact]
    public void AtWrapper_ScheduleTask_ThrowsIfJobIdIsNotInteger()
    {
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        _settingsServiceMock.Setup(sm => sm.SchedulerSettings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var atWrapper = new AtWrapper(_filesystemMock.Object, _processRunnerMock.Object, _settingsServiceMock.Object);

        var task = new ScheduledTask(Guid.NewGuid(), "Test Task", "echo Hello World", DateTimeOffset.UtcNow.AddMinutes(10), "{}");

        _processRunnerMock.Setup(pr => pr.RunProcess(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(("abc", "", 0));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => atWrapper.ScheduleTask(task));
        Assert.Equal("Failed to parse job ID from output: abc", exception.Message);
    }

    [Fact]
    public void AtWrapper_GetTaskDetails_ReturnsTaskDetails()
    {
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        _settingsServiceMock.Setup(sm => sm.SchedulerSettings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var atWrapper = new AtWrapper(_filesystemMock.Object, _processRunnerMock.Object, _settingsServiceMock.Object);

        var jobId = 123;
        var task = new ScheduledTask(Guid.NewGuid(), "Test Task", "echo Hello World", DateTimeOffset.UtcNow.AddMinutes(10), "{}");
        var taskJson = JsonSerializer.Serialize(task);

        _processRunnerMock.Setup(pr => pr.RunProcess("at", $"-c {jobId}"))
           .Returns(($"SOMETHING=\nTASK_ID={task.Id}\nTEST=\n", string.Empty, 0));
        _filesystemMock.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(true);
        _filesystemMock.Setup(fs => fs.File.Exists(It.IsAny<string>())).Returns(true);
        _filesystemMock.Setup(fs => fs.File.Move(It.IsAny<string>(), It.IsAny<string>()));
        _filesystemMock.Setup(fs => fs.File.Delete(It.IsAny<string>()));
        string expected = $"export TASK_ID={task.Id}\n" + "export TASK_DEFINITION=" + taskJson + "\nexport TASK_INNER_DEFINITION={}\n";
        _filesystemMock.Setup(f => f.File.ReadAllText(It.IsAny<string>())).Returns(expected);


        // Act
        var (returnedJobId, newtask) = atWrapper.GetTaskDetails(jobId);

        // Assert
        Assert.Equal(jobId, returnedJobId);
        Assert.NotNull(task);
        Assert.Equal(task.Id, newtask.Id);
        Assert.Equal(task.Name, newtask.Name);
        Assert.Equal(task.Command, newtask.Command);
        Assert.Equal(task.StartTime, newtask.StartTime);
        Assert.Equal(task.InnerScheduledTask, newtask.InnerScheduledTask);
    }

    [Fact]
    public void AtWrapper_GetTaskDetails_OutputEmpty_Throws()
    {
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        _settingsServiceMock.Setup(sm => sm.SchedulerSettings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var atWrapper = new AtWrapper(_filesystemMock.Object, _processRunnerMock.Object, _settingsServiceMock.Object);

        var jobId = 123;

        _processRunnerMock.Setup(pr => pr.RunProcess(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(("", "", 0));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => atWrapper.GetTaskDetails(jobId));
        Assert.Equal($"No output from at -c for job {jobId}", exception.Message);
    }

    [Fact]
    public void AtWrapper_GetTaskDetails_InvalidTaskId_Throws()
    {
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        _settingsServiceMock.Setup(sm => sm.SchedulerSettings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var atWrapper = new AtWrapper(_filesystemMock.Object, _processRunnerMock.Object, _settingsServiceMock.Object);

        var jobId = 123;
        var task = new ScheduledTask(Guid.NewGuid(), "Test Task", "echo Hello World", DateTimeOffset.UtcNow.AddMinutes(10), "{}");
        var taskJson = JsonSerializer.Serialize(task);

        _processRunnerMock.Setup(pr => pr.RunProcess("at", $"-c {jobId}"))
           .Returns(($"SOMETHING=\nTASK_ID=ABC_DEF\nTEST=\n", string.Empty, 0));
        _filesystemMock.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(true);
        _filesystemMock.Setup(fs => fs.File.Exists(It.IsAny<string>())).Returns(true);
        _filesystemMock.Setup(fs => fs.File.Move(It.IsAny<string>(), It.IsAny<string>()));
        _filesystemMock.Setup(fs => fs.File.Delete(It.IsAny<string>()));
        string expected = $"export TASK_ID={task.Id}\n" + "export TASK_DEFINITION=" + taskJson + "\nexport TASK_INNER_DEFINITION={}\n";
        _filesystemMock.Setup(f => f.File.ReadAllText(It.IsAny<string>())).Returns(expected);


        // Act
        Assert.Throws<InvalidOperationException>(() => atWrapper.GetTaskDetails(jobId));
    }

    [Fact]
    public void AtWrapper_GetTaskDetails_InvalidJson_Throws()
    {
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        _settingsServiceMock.Setup(sm => sm.SchedulerSettings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var atWrapper = new AtWrapper(_filesystemMock.Object, _processRunnerMock.Object, _settingsServiceMock.Object);

        var jobId = 123;
        var task = new ScheduledTask(Guid.NewGuid(), "Test Task", "echo Hello World", DateTimeOffset.UtcNow.AddMinutes(10), "{}");
        var taskJson = JsonSerializer.Serialize(task);

        _processRunnerMock.Setup(pr => pr.RunProcess("at", $"-c {jobId}"))
           .Returns(($"SOMETHING=\nTASK_ID={task.Id}\nTEST=\n", string.Empty, 0));
        _filesystemMock.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(true);
        _filesystemMock.Setup(fs => fs.File.Exists(It.IsAny<string>())).Returns(true);
        _filesystemMock.Setup(fs => fs.File.Move(It.IsAny<string>(), It.IsAny<string>()));
        _filesystemMock.Setup(fs => fs.File.Delete(It.IsAny<string>()));
        string expected = $"export TASK_ID={task.Id}\n" + "export TASK_DEFINITION={{{{}\nexport TASK_INNER_DEFINITION={}\n";
        _filesystemMock.Setup(f => f.File.ReadAllText(It.IsAny<string>())).Returns(expected);


        // Act
        Assert.Throws<InvalidOperationException>(() => atWrapper.GetTaskDetails(jobId));
    }

    [Fact]
    public void AtWrapper_GetTaskDetails_NullJson_Throws()
    {
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        _settingsServiceMock.Setup(sm => sm.SchedulerSettings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var atWrapper = new AtWrapper(_filesystemMock.Object, _processRunnerMock.Object, _settingsServiceMock.Object);

        var jobId = 123;
        var task = new ScheduledTask(Guid.NewGuid(), "Test Task", "echo Hello World", DateTimeOffset.UtcNow.AddMinutes(10), "{}");
        var taskJson = JsonSerializer.Serialize(task);

        _processRunnerMock.Setup(pr => pr.RunProcess("at", $"-c {jobId}"))
           .Returns(($"SOMETHING=\nTASK_ID={task.Id}\nTEST=\n", string.Empty, 0));
        _filesystemMock.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(true);
        _filesystemMock.Setup(fs => fs.File.Exists(It.IsAny<string>())).Returns(true);
        _filesystemMock.Setup(fs => fs.File.Move(It.IsAny<string>(), It.IsAny<string>()));
        _filesystemMock.Setup(fs => fs.File.Delete(It.IsAny<string>()));
        string expected = $"export TASK_ID={task.Id}\n" + "export TASK_DEFINITION=null\nexport TASK_INNER_DEFINITION={}\n";
        _filesystemMock.Setup(f => f.File.ReadAllText(It.IsAny<string>())).Returns(expected);


        // Act
        Assert.Throws<InvalidOperationException>(() => atWrapper.GetTaskDetails(jobId));
    }

    [Fact]
    public void AtWrapper_GetTaskDetails_InvalidId_Throws()
    {
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        _settingsServiceMock.Setup(sm => sm.SchedulerSettings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var atWrapper = new AtWrapper(_filesystemMock.Object, _processRunnerMock.Object, _settingsServiceMock.Object);

        var jobId = 123;

        _processRunnerMock.Setup(pr => pr.RunProcess(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(("TASK_ID=abc", "", 0));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => atWrapper.GetTaskDetails(jobId));
    }

    [Fact]
    public void AtWrapper_GetTaskDetails_FailsIfEnvironmentNotSet()
    {
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        _settingsServiceMock.Setup(sm => sm.SchedulerSettings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var atWrapper = new AtWrapper(_filesystemMock.Object, _processRunnerMock.Object, _settingsServiceMock.Object);

        var jobId = 123;

        _processRunnerMock.Setup(pr => pr.RunProcess(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((jobId.ToString(), "\n", 0));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => atWrapper.GetTaskDetails(jobId));
        Assert.Equal($"Task ID not found in output.", exception.Message);
    }

    [Fact]
    public void AtWrapper_GetTaskDetails_FailsIfTaskNotDeserialized()
    {
        // Arrange
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        _settingsServiceMock.Setup(sm => sm.SchedulerSettings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var atWrapper = new AtWrapper(_filesystemMock.Object, _processRunnerMock.Object, _settingsServiceMock.Object);
        var task = new ScheduledTask(Guid.NewGuid(), "Test Task", "echo Hello World", DateTimeOffset.UtcNow.AddMinutes(10), "{}");
        var jobId = 123;
        var taskJson = JsonSerializer.Serialize(task);
        _processRunnerMock.Setup(pr => pr.RunProcess("at", It.IsAny<string>()))

            .Returns(($"export TASK_ID={task.Id}\n", string.Empty, 0));
        _filesystemMock.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(true);
        _filesystemMock.Setup(fs => fs.File.Exists(It.IsAny<string>())).Returns(true);
        _filesystemMock.Setup(fs => fs.File.Move(It.IsAny<string>(), It.IsAny<string>()));
        _filesystemMock.Setup(fs => fs.File.Delete(It.IsAny<string>()));
        string expected = $"export TASK_ID={task.Id}\n" + "export TASK_DEFINITION=\nexport TASK_INNER_DEFINITION={}\n";
        _filesystemMock.Setup(f => f.File.ReadAllText(It.IsAny<string>())).Returns(expected);

        // Act & Assert
        var _ = Assert.Throws<InvalidOperationException>(() => atWrapper.GetTaskDetails(jobId));
    }

    [Fact]
    public void AtWrapper_GetTaskDetails_ThrowsIfAtqReturnsErrorCode()
    {
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        _settingsServiceMock.Setup(sm => sm.SchedulerSettings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var atWrapper = new AtWrapper(_filesystemMock.Object, _processRunnerMock.Object, _settingsServiceMock.Object);

        var jobId = 123;
        var expectedError = "Error occurred";

        _processRunnerMock.Setup(pr => pr.RunProcess(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(("", expectedError, 1));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => atWrapper.GetTaskDetails(jobId));
        Assert.Equal($"Failed to get job details for job {jobId}: {expectedError}", exception.Message);
    }

    [Fact]
    public void AtWrapper_GetTaskDetails_TaskDefinitionEmpty_Throws()
    {
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        _settingsServiceMock.Setup(sm => sm.SchedulerSettings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var atWrapper = new AtWrapper(_filesystemMock.Object, _processRunnerMock.Object, _settingsServiceMock.Object);

        var jobId = 123;
        var task = new ScheduledTask(Guid.NewGuid(), "Test Task", "echo Hello World", DateTimeOffset.UtcNow.AddMinutes(10), "{}");
        var taskJson = JsonSerializer.Serialize(task);
        _processRunnerMock.Setup(pr => pr.RunProcess("at", $"-c {jobId}"))
            .Returns(($"TASK_ID={task.Id}\n", string.Empty, 0));
        _filesystemMock.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(true);
        _filesystemMock.Setup(fs => fs.File.Exists(It.IsAny<string>())).Returns(true);
        _filesystemMock.Setup(fs => fs.File.Move(It.IsAny<string>(), It.IsAny<string>()));
        _filesystemMock.Setup(fs => fs.File.Delete(It.IsAny<string>()));
        string expected = $"export TASK_ID={task.Id}\n" + "export TASK_DEFINITION=\nexport TASK_INNER_DEFINITION={}\n";
        _filesystemMock.Setup(f => f.File.ReadAllText(It.IsAny<string>())).Returns(expected);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => atWrapper.GetTaskDetails(jobId));
    }

    [Fact]
    public void AtWrapper_GetTaskDetails_JsonException_Throws()
    {
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        _settingsServiceMock.Setup(sm => sm.SchedulerSettings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var atWrapper = new AtWrapper(_filesystemMock.Object, _processRunnerMock.Object, _settingsServiceMock.Object);

        var jobId = 123;
        var task = new ScheduledTask(Guid.NewGuid(), "Test Task", "echo Hello World", DateTimeOffset.UtcNow.AddMinutes(10), "{}");
        var taskJson = JsonSerializer.Serialize(task); _processRunnerMock.Setup(pr => pr.RunProcess("at", $"-c {jobId}"))
            .Returns(($"TASK_ID={task.Id}\n", string.Empty, 0));
        _filesystemMock.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(true);
        _filesystemMock.Setup(fs => fs.File.Exists(It.IsAny<string>())).Returns(true);
        _filesystemMock.Setup(fs => fs.File.Move(It.IsAny<string>(), It.IsAny<string>()));
        _filesystemMock.Setup(fs => fs.File.Delete(It.IsAny<string>()));
        string expected = $"export TASK_ID={task.Id}\n" + "export TASK_DEFINITION={null}\nexport TASK_INNER_DEFINITION={}\n";
        _filesystemMock.Setup(f => f.File.ReadAllText(It.IsAny<string>())).Returns(expected);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => atWrapper.GetTaskDetails(jobId));
    }
}