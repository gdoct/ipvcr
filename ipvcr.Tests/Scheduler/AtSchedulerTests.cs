using ipvcr.Logic.Api;
using ipvcr.Logic.Scheduler;
using ipvcr.Logic.Settings;
using Moq;
using System.IO.Abstractions;
using System.Text.Json;

namespace ipvcr.Tests.Scheduler;

public class AtSchedulerTests
{
    private MockRepository MockRepository { get; } = new MockRepository(MockBehavior.Strict);

    [Fact]
    public void AtScheduler_Ctor_Valid()
    {
        // Arrange
        var fileSystem = MockRepository.Create<IFileSystem>();
        var processRunner = MockRepository.Create<IProcessRunner>();
        var settingsService = MockRepository.Create<ISettingsService>();
        processRunner.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        processRunner.Setup(pr => pr.RunProcess("which", "atq")).Returns(("atq", "", 0));
        processRunner.Setup(pr => pr.RunProcess("which", "atrm")).Returns(("atrm", "", 0));
        settingsService.Setup(sm => sm.SchedulerSettings).Returns(new SchedulerSettings { DataPath = "/tmp" });

        // Act
        var scheduler = new AtScheduler(fileSystem.Object, processRunner.Object, settingsService.Object);

        // Assert
        Assert.NotNull(scheduler);
    }

    [Fact]
    public void AtScheduler_FetchScheduledTasks_Valid()
    {
        // Arrange
        var fileSystem = MockRepository.Create<IFileSystem>();
        var processRunner = MockRepository.Create<IProcessRunner>();
        var settingsService = MockRepository.Create<ISettingsService>();
        processRunner.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        processRunner.Setup(pr => pr.RunProcess("which", "atq")).Returns(("atq", "", 0));
        processRunner.Setup(pr => pr.RunProcess("which", "atrm")).Returns(("atrm", "", 0));
        settingsService.Setup(sm => sm.SchedulerSettings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var task = new ScheduledTask(Guid.NewGuid(), "Test Task", "echo Hello World", DateTimeOffset.UtcNow.AddMinutes(10), "{}");
        var taskJson = JsonSerializer.Serialize(task);
        var scheduler = new AtScheduler(fileSystem.Object, processRunner.Object, settingsService.Object);
        var expectedOutput = "1 2023-10-01 12:00 a\n2 2023-10-02 14:00 a\n";
        processRunner.Setup(pr => pr.RunProcess("atq", It.IsAny<string>()))
            .Returns((expectedOutput, "", 0));
        var taskId = Guid.NewGuid();
        processRunner.Setup(p => p.RunProcess("at", "-c 1")).Returns(($"TASK_ID={taskId}\n", "", 0));
        processRunner.Setup(p => p.RunProcess("at", "-c 2")).Returns(($"TASK_ID={taskId}\n", "", 0));
        // Act
        var fileMock = MockRepository.Create<IFile>();
        fileSystem.SetupGet(fs => fs.File).Returns(fileMock.Object);
        fileMock.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
        fileMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns($"export TASK_DEFINITION={taskJson}\nexport TASK_INNER_DEFINITION=\n");
        // Assert
        var tasks = scheduler.FetchScheduledTasks().ToList();
        Assert.NotNull(tasks);
        var count = tasks.Count;
        MockRepository.VerifyAll();
        Assert.Equal(2, count);
    }

    [Fact]
    public void AtScheduler_FetchScheduledTasks_Empty()
    {
        // Arrange
        var fileSystem = MockRepository.Create<IFileSystem>();
        var processRunner = MockRepository.Create<IProcessRunner>();
        var settingsService = MockRepository.Create<ISettingsService>();
        processRunner.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        processRunner.Setup(pr => pr.RunProcess("which", "atq")).Returns(("atq", "", 0));
        processRunner.Setup(pr => pr.RunProcess("which", "atrm")).Returns(("atrm", "", 0));
        settingsService.Setup(sm => sm.SchedulerSettings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var scheduler = new AtScheduler(fileSystem.Object, processRunner.Object, settingsService.Object);
        var expectedOutput = "";
        processRunner.Setup(pr => pr.RunProcess("atq", It.IsAny<string>()))
            .Returns((expectedOutput, "", 0));
        var fileMock = MockRepository.Create<IFile>();
        fileSystem.SetupGet(fs => fs.File).Returns(fileMock.Object);
        fileMock.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
        var id = Guid.NewGuid();
        fileMock.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns($"export TASK_ID={id}\nexport TASK_DEFINITION=\nexport TASK_INNER_DEFINITION=\n");

        // Act
        var tasks = scheduler.FetchScheduledTasks().ToList();

        // Assert
        Assert.NotNull(tasks);
        Assert.Empty(tasks);
    }

    [Fact]
    public void AtScheduler_CancelTask_Valid()
    {
        // Arrange
        var fileSystem = MockRepository.Create<IFileSystem>();
        var processRunner = MockRepository.Create<IProcessRunner>();
        var settingsService = MockRepository.Create<ISettingsService>();
        processRunner.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        processRunner.Setup(pr => pr.RunProcess("which", "atq")).Returns(("atq", "", 0));
        processRunner.Setup(pr => pr.RunProcess("which", "atrm")).Returns(("atrm", "", 0));
        settingsService.Setup(sm => sm.SchedulerSettings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var scheduler = new AtScheduler(fileSystem.Object, processRunner.Object, settingsService.Object);
        var taskId = Guid.NewGuid();
        var jobId = 123;
        var task = new ScheduledTask(taskId, "Test Task", "echo Hello World", DateTimeOffset.UtcNow.AddMinutes(10), "{}");
        var taskJson = JsonSerializer.Serialize(task);
        processRunner.Setup(pr => pr.RunProcess("atq", It.IsAny<string>()))
            .Returns((jobId.ToString() + "\tinfo more info", "", 0));

        var id = Guid.NewGuid();
        processRunner.Setup(p => p.RunProcess("at", "-c " + jobId)).Returns(($"TASK_ID={id}\n", "", 0));
        processRunner.Setup(p => p.RunProcess("atrm", jobId.ToString())).Returns(("", "", 0));
        fileSystem.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(true);
        fileSystem.Setup(fs => fs.File.Exists(It.IsAny<string>())).Returns(true);
        fileSystem.Setup(fs => fs.File.Move(It.IsAny<string>(), It.IsAny<string>()));
        fileSystem.Setup(fs => fs.File.Delete(It.IsAny<string>()));
        fileSystem.Setup(f => f.File.ReadAllText(It.IsAny<string>())).Returns($"export TASK_ID={id}\n" + "export TASK_DEFINITION=" + taskJson + "\nexport TASK_INNER_DEFINITION={}\n");

        // Act
        scheduler.CancelTask(taskId);

        // Assert
        processRunner.Verify(p => p.RunProcess("atrm", jobId.ToString()), Times.Once);
    }

    [Fact]
    public void AtScheduler_CancelTask_NotFound_Throws()
    {
        // Arrange
        var fileSystem = MockRepository.Create<IFileSystem>();
        var processRunner = MockRepository.Create<IProcessRunner>();
        var settingsService = MockRepository.Create<ISettingsService>();
        processRunner.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        processRunner.Setup(pr => pr.RunProcess("which", "atq")).Returns(("atq", "", 0));
        processRunner.Setup(pr => pr.RunProcess("which", "atrm")).Returns(("atrm", "", 0));
        settingsService.Setup(sm => sm.SchedulerSettings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var scheduler = new AtScheduler(fileSystem.Object, processRunner.Object, settingsService.Object);
        var taskId = Guid.NewGuid();
        processRunner.Setup(pr => pr.RunProcess("atq", string.Empty)).Returns(("", "", 0));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => scheduler.CancelTask(taskId));
    }

    [Fact]
    public void AtScheduler_GetTaskDefinition_NotFound_Throws()
    {
        // Arrange
        var fileSystem = MockRepository.Create<IFileSystem>();
        var processRunner = MockRepository.Create<IProcessRunner>();
        var settingsService = MockRepository.Create<ISettingsService>();
        processRunner.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        processRunner.Setup(pr => pr.RunProcess("which", "atq")).Returns(("atq", "", 0));
        processRunner.Setup(pr => pr.RunProcess("which", "atrm")).Returns(("atrm", "", 0));
        settingsService.Setup(sm => sm.SchedulerSettings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        processRunner.Setup(pr => pr.RunProcess("atq", string.Empty)).Returns(("", "", 0));
        var scheduler = new AtScheduler(fileSystem.Object, processRunner.Object, settingsService.Object);
        var taskId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => scheduler.GetTaskDefinition(taskId));
    }

    [Fact]
    public void AtScheduler_GetTaskDefinition_ReturnsInnerScheduledTask()
    {
        // Arrange
        var fileSystem = MockRepository.Create<IFileSystem>();
        var processRunner = MockRepository.Create<IProcessRunner>();
        var settingsService = MockRepository.Create<ISettingsService>();
        processRunner.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        processRunner.Setup(pr => pr.RunProcess("which", "atq")).Returns(("atq", "", 0));
        processRunner.Setup(pr => pr.RunProcess("which", "atrm")).Returns(("atrm", "", 0));
        settingsService.Setup(sm => sm.SchedulerSettings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var scheduler = new AtScheduler(fileSystem.Object, processRunner.Object, settingsService.Object);
        var taskId = Guid.NewGuid();
        var jobId = 123;
        var task = new ScheduledTask(taskId, "Test Task", "echo Hello World", DateTimeOffset.UtcNow.AddMinutes(10), "{}");
        var taskJson = JsonSerializer.Serialize(task);
        processRunner.Setup(pr => pr.RunProcess("atq", It.IsAny<string>()))
            .Returns((jobId.ToString() + "\tinfo more info", "", 0));

        processRunner.Setup(p => p.RunProcess("at", "-c " + jobId)).Returns(($"TASK_ID={taskId}\n", "", 0));
        fileSystem.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(true);
        fileSystem.Setup(fs => fs.File.Exists(It.IsAny<string>())).Returns(true);
        fileSystem.Setup(fs => fs.File.Move(It.IsAny<string>(), It.IsAny<string>()));
        fileSystem.Setup(fs => fs.File.Delete(It.IsAny<string>()));
        string expected = $"export TASK_ID={taskId}\n" + "export TASK_DEFINITION=" + taskJson + "\nexport TASK_INNER_DEFINITION={}\n";
        fileSystem.Setup(f => f.File.ReadAllText(It.IsAny<string>())).Returns(expected);

        // Act
        var result = scheduler.GetTaskDefinition(taskId);

        // Assert
        Assert.Equal(result, expected);
    }

    [Fact]
    public void AtScheduler_UpdateTaskDefinition_Valid()
    {
        // Arrange
        var fileSystem = MockRepository.Create<IFileSystem>();
        var processRunner = MockRepository.Create<IProcessRunner>();
        var settingsService = MockRepository.Create<ISettingsService>();
        processRunner.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        processRunner.Setup(pr => pr.RunProcess("which", "atq")).Returns(("atq", "", 0));
        processRunner.Setup(pr => pr.RunProcess("which", "atrm")).Returns(("atrm", "", 0));
        settingsService.Setup(sm => sm.SchedulerSettings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var scheduler = new AtScheduler(fileSystem.Object, processRunner.Object, settingsService.Object);
        var taskId = Guid.NewGuid();
        var jobId = 123;
        var task = new ScheduledTask(taskId, "Test Task", "echo Hello World", DateTimeOffset.UtcNow.AddMinutes(10), "{}");
        var taskJson = JsonSerializer.Serialize(task);
        processRunner.Setup(pr => pr.RunProcess("atq", It.IsAny<string>()))
            .Returns((jobId.ToString() + "\tinfo more info", "", 0));
        settingsService.SetupGet(s => s.SchedulerSettings).Returns(new SchedulerSettings { DataPath = "/data/path" });
        var taskScriptManager = new TaskScriptManager(fileSystem.Object, settingsService.Object);
        var file = MockRepository.Create<IFile>();
        fileSystem.SetupGet(fs => fs.File).Returns(file.Object);
        file.Setup(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()));
        file.Setup(f => f.SetAttributes(It.IsAny<string>(), It.IsAny<FileAttributes>())).Verifiable();

        processRunner.Setup(p => p.RunProcess("at", "-c " + jobId)).Returns(($"TASK_ID={taskId}\n", "", 0));
        fileSystem.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(true);
        fileSystem.Setup(fs => fs.File.Exists(It.IsAny<string>())).Returns(true);
        fileSystem.Setup(fs => fs.File.Move(It.IsAny<string>(), It.IsAny<string>()));
        fileSystem.Setup(fs => fs.File.Delete(It.IsAny<string>()));
        string expected = $"export TASK_ID={taskId}\n" + "export TASK_DEFINITION=" + taskJson + "\nexport TASK_INNER_DEFINITION={}\n";
        fileSystem.Setup(f => f.File.ReadAllText(It.IsAny<string>())).Returns(expected);

        // Act
        scheduler.UpdateTaskDefinition(taskId, taskJson);

        // Assert
        processRunner.Verify(p => p.RunProcess("at", "-c " + jobId), Times.Once);
    }

    [Fact]
    public void AtScheduler_UpdateTaskDefinition_TaskNotFound()
    {
        // Arrange
        var fileSystem = MockRepository.Create<IFileSystem>();
        var processRunner = MockRepository.Create<IProcessRunner>();
        var settingsService = MockRepository.Create<ISettingsService>();
        processRunner.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        processRunner.Setup(pr => pr.RunProcess("which", "atq")).Returns(("atq", "", 0));
        processRunner.Setup(pr => pr.RunProcess("which", "atrm")).Returns(("atrm", "", 0));
        settingsService.Setup(sm => sm.SchedulerSettings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var scheduler = new AtScheduler(fileSystem.Object, processRunner.Object, settingsService.Object);
        var taskId = Guid.NewGuid();
        processRunner.Setup(pr => pr.RunProcess("atq", string.Empty)).Returns(("", "", 0));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => scheduler.UpdateTaskDefinition(taskId, "{}"));
    }

    [Fact]
    public void AtScheduler_ScheduleTask_CallsAtWrapperAndWritesTaskScript()
    {
        // Arrange
        var fileSystem = MockRepository.Create<IFileSystem>();
        var processRunner = MockRepository.Create<IProcessRunner>();
        var settingsService = MockRepository.Create<ISettingsService>();
        processRunner.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        processRunner.Setup(pr => pr.RunProcess("which", "atq")).Returns(("atq", "", 0));
        processRunner.Setup(pr => pr.RunProcess("which", "atrm")).Returns(("atrm", "", 0));
        settingsService.Setup(sm => sm.SchedulerSettings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var scheduler = new AtScheduler(fileSystem.Object, processRunner.Object, settingsService.Object);
        var taskId = Guid.NewGuid();
        var task = new ScheduledTask(taskId, "Test Task", "echo Hello World", DateTimeOffset.UtcNow.AddMinutes(10), "{}");
        var taskJson = JsonSerializer.Serialize(task);
        var taskScriptManager = new TaskScriptManager(fileSystem.Object, settingsService.Object);
        var file = MockRepository.Create<IFile>();
        fileSystem.SetupGet(fs => fs.File).Returns(file.Object);
        file.Setup(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()));
        file.Setup(f => f.SetAttributes(It.IsAny<string>(), It.IsAny<FileAttributes>())).Verifiable();
        // ProcessRunner.RunProcess("/bin/bash", "-c "echo "/tmp/tasks/e96590c9-3b79-41e7-9084-4295249d2c19.sh" | at 16:38 04/16/2025"") 
        processRunner.Setup(p => p.RunProcess("/bin/bash", It.IsAny<string>())).Returns(("123", "", 0));
        // Act
        scheduler.ScheduleTask(task);

        // Assert
        MockRepository.VerifyAll();
    }
}