// Purpose: This file contains the tests for the SchedulingContext class.
using ipvcr.Logic;
using ipvcr.Logic.Api;
using ipvcr.Logic.Scheduler;
using ipvcr.Logic.Settings;
using Moq;

namespace ipvcr.Tests.Scheduler;

public class SchedulingContextTests
{
    [Fact]
    // test that the enumeration only offers recordings, even if more are present in the task manager's list
    public void SchedulingContext_GetRecordings_OnlyReturnsRecordings()
    {
        // Arrange
        var taskScheduler = new Mock<ITaskScheduler>();
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.FfmpegSettings).Returns(new FfmpegSettings());
        var json = System.Text.Json.JsonSerializer.Serialize(new ScheduledRecording(Guid.NewGuid(), "Task 1", "", "filename", "http://whatevah", "", DateTime.Now, DateTime.Now.AddHours(1)));
        taskScheduler.Setup(s => s.FetchScheduledTasks()).Returns(
        [
            new ScheduledTask(Guid.NewGuid(), "Task 1", "ffmpeg -i http://example.com/stream -t 3600 -c copy -f mp4 output.mp4", DateTime.Now, json),
            new ScheduledTask(Guid.NewGuid(), "Task 2", "ffmpeg -i http://example.com/stream -t 3600 -c copy -f mp4 output.mp4", DateTime.Now, json),
        ]);

        var context = new RecordingSchedulingContext(taskScheduler.Object, settingsService.Object);

        // Act
        var recordings = context.Recordings;

        // Assert
        Assert.Equal(2, recordings.Count());
    }

    [Fact]
    public void SchedulingContext_AddRecording_SchedulesTask()
    {
        // Arrange
        var taskScheduler = new Mock<ITaskScheduler>();
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.FfmpegSettings).Returns(new FfmpegSettings());
        var context = new RecordingSchedulingContext(taskScheduler.Object, settingsService.Object);

        // Act
        var recording = new ScheduledRecording(Guid.NewGuid(), "Task 1", "", "filename", "http://whatevah", "", DateTime.Now, DateTime.Now.AddHours(1));
        context.AddRecording(recording);

        // Assert
        taskScheduler.Verify(s => s.ScheduleTask(It.IsAny<ScheduledTask>()));
    }

    [Fact]
    public void SchedulingContext_RemoveRecording_UnschedulesTask()
    {
        // Arrange
        var taskScheduler = new Mock<ITaskScheduler>();
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.FfmpegSettings).Returns(new FfmpegSettings());

        var context = new RecordingSchedulingContext(taskScheduler.Object, settingsService.Object);
        var recording = new ScheduledRecording(Guid.NewGuid(), "Task 1", "", "filename", "http://whatevah", "", DateTime.Now, DateTime.Now.AddHours(1));

        // Act
        context.AddRecording(recording);
        context.RemoveRecording(recording.Id);

        // Assert
        taskScheduler.Verify(s => s.CancelTask(recording.Id));
    }


    [Fact]
    public void SchedulingContext_GetTaskDefinition_ReturnsCorrectDefinition()
    {
        // Arrange
        var taskScheduler = new Mock<ITaskScheduler>();
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.FfmpegSettings).Returns(new FfmpegSettings());

        var context = new RecordingSchedulingContext(taskScheduler.Object, settingsService.Object);
        var recordingId = Guid.NewGuid();
        var expectedDefinition = "ffmpeg -i http://example.com/stream -t 3600 -c copy -f mp4 output.mp4";

        taskScheduler.Setup(s => s.GetTaskDefinition(recordingId)).Returns(expectedDefinition);

        // Act
        var result = context.GetTaskDefinition(recordingId);

        // Assert
        Assert.Equal(expectedDefinition, result);
    }

    [Fact]
    public void SchedulingContext_UpdateTaskDefinition_UpdatesDefinition()
    {
        // Arrange
        var taskScheduler = new Mock<ITaskScheduler>();
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.FfmpegSettings).Returns(new FfmpegSettings());

        var context = new RecordingSchedulingContext(taskScheduler.Object, settingsService.Object);
        var recordingId = Guid.NewGuid();
        var newDefinition = "ffmpeg -i http://example.com/stream -t 7200 -c copy -f mp4 output.mp4";

        // Act
        context.UpdateTaskDefinition(recordingId, newDefinition);

        // Assert
        taskScheduler.Verify(s => s.UpdateTaskDefinition(recordingId, newDefinition));
    }
}