using ipvcr.Logic;
using ipvcr.Logic.Scheduler;
using ipvcr.Logic.Settings;

namespace ipvcr.Tests.Scheduler;

public class ScheduledTaskTests
{
    [Fact]
    public void ScheduledTask_FromScheduledTask_ShouldSucceedConversion()
    {
        var id = Guid.NewGuid();
        var name = "name";
        var filename = "filename";
        var channelUri = "http://channel.uri.org/abc/def/1234";
        var startTime = DateTime.Now;
        var endTime = DateTime.Now.AddSeconds(10);
        var command = $"ffmpeg -i {channelUri} -t {Convert.ToInt32((endTime - startTime).TotalSeconds)} -c copy -f mp4 {filename}";
        var schedrec = new ScheduledRecording(id, name, "", filename, channelUri, "", startTime, endTime);
        var json = System.Text.Json.JsonSerializer.Serialize(schedrec);
        var scheduledTask = new ScheduledTask(id, name, command, startTime, json);
        var result = ScheduledRecording.FromScheduledTask(scheduledTask);
        Assert.Equal(id, result.Id);
        Assert.Equal(name, result.Name);
        Assert.Equal(filename, result.Filename);
        Assert.Equal(channelUri, result.ChannelUri);
        Assert.Equal(startTime.ToString("yyyy-MM-dd HH:mm:ss"), result.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
        Assert.Equal(endTime.ToString("yyyy-MM-dd HH:mm:ss"), result.EndTime.ToString("yyyy-MM-dd HH:mm:ss"));
        Assert.NotEqual(schedrec.ChannelUri, schedrec.Obfuscate());
        Assert.Equal("http://chan.../1234", schedrec.Obfuscate());
    }

    [Theory]
    [InlineData("{\"Id\":\"855e1099-8046-4811-80c0-b64501ed8251\",\"TaskId\":0,\"Name\":\"wa\",\"Command\":\"ffmpeg -i http://mag.diamondtv.top/crS7aWh0/p47Taxa/2998 -t 3600 -c copy -f mp4 -metadata title=\\u0022wa\\u0022 -metadata description=\\u0022wa - recorded from RTL 4 at Apr 17 2025, 04:04 PM\\u0022 /media/wa_20250417_16041604.mp4\",\"StartTime\":\"2025-04-17T16:04:00+02:00\",\"InnerScheduledTask\":\"\"}"
)]
    public void ScheduledTaskDeserialize_Valid(string json)
    {
        var task = System.Text.Json.JsonSerializer.Deserialize<ScheduledTask>(json);
        Assert.NotNull(task);
        Assert.NotEmpty(task.Name);
        Assert.NotEmpty(task.Command);
        Assert.Empty(task.InnerScheduledTask);
    }

    [Fact]
    public void ScheduledTask_ToAndFromScheduledRecording_ShouldSucceedConversion()
    {
        var id = Guid.NewGuid();
        var name = "name";
        var filename = "filename";
        var channelUri = "http://channelUri";
        var startTime = DateTime.Now;
        var endTime = DateTime.Now.AddSeconds(10);
        var scheduledRecording = new ScheduledRecording(id, name, "", filename, channelUri, "", startTime, endTime);
        var scheduledTask = scheduledRecording.ToScheduledTask(new FfmpegSettings());
        var result = ScheduledRecording.FromScheduledTask(scheduledTask);
        Assert.Equal(id, result.Id);
        Assert.Equal(name, result.Name);
        Assert.Equal(filename, result.Filename);
        Assert.Equal(channelUri, result.ChannelUri);
        Assert.Equal(startTime.ToString("yyyy-MM-dd HH:mm:ss"), result.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
        Assert.Equal(endTime.ToString("yyyy-MM-dd HH:mm:ss"), result.EndTime.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    [Fact]
    public void ScheduledTask_Conversion_ThrowsWhenInnerScheduledTaskIsEmptyOrInvalid()
    {
        var task = new ScheduledTask(Guid.NewGuid(), "name", "command", DateTime.Now, string.Empty);
        Assert.Throws<Exception>(() => ScheduledRecording.FromScheduledTask(task));
        var task2 = new ScheduledTask(Guid.NewGuid(), "name", "command", DateTime.Now, "invalid_json");
        Assert.Throws<Exception>(() => ScheduledRecording.FromScheduledTask(task2));
    }

    [Fact]
    public void ScheduledRecording_SerializeToJsonInTask_ShouldBeCorrect()
    {
        var id = Guid.NewGuid();
        var name = "name";
        var description = "blabla";
        var filename = "filename";
        var channelUri = "http://channelUri";
        var channelName = "name";
        var startTime = DateTime.Now;
        var endTime = DateTime.Now.AddSeconds(10);
        var scheduledRecording = new ScheduledRecording(id, name, description, filename, channelUri, channelName, startTime, endTime);


        var task = scheduledRecording.ToScheduledTask(new());
        var innerjson = task.InnerScheduledTask;
        Assert.NotNull(innerjson);
        var innerScheduledRecording = System.Text.Json.JsonSerializer.Deserialize<ScheduledRecording>(innerjson);
        Assert.NotNull(innerScheduledRecording);
        Assert.Equal(id, innerScheduledRecording.Id);
        Assert.Equal(name, innerScheduledRecording.Name);
        Assert.Equal(description, innerScheduledRecording.Description);
        Assert.Equal(filename, innerScheduledRecording.Filename);
        Assert.Equal(channelUri, innerScheduledRecording.ChannelUri);
        Assert.Equal(channelName, innerScheduledRecording.ChannelName);
        Assert.Equal(startTime.ToString("yyyy-MM-dd HH:mm:ss"), innerScheduledRecording.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
        Assert.Equal(endTime.ToString("yyyy-MM-dd HH:mm:ss"), innerScheduledRecording.EndTime.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    [Fact]
    public void GenerateFfMpegCommandString_WithAllDefaultSettings_AppliesCopyCodecs()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Test Recording";
        var description = "Test Description";
        var filename = "/media/test_recording.mp4";
        var channelUri = "http://example.com/stream1";
        var startTime = DateTimeOffset.Now;
        var endTime = startTime.AddHours(1);
        var recording = new ScheduledRecording(id, name, description, filename, channelUri, "Test Channel", startTime, endTime);

        // Create settings with empty strings to test default behavior
        var ffmpegSettings = new FfmpegSettings
        {
            Codec = "",
            AudioCodec = "",
            VideoBitrate = "",
            AudioBitrate = "",
            Resolution = "",
            FrameRate = "",
            AspectRatio = "",
            OutputFormat = "",
            FileType = ""
        };

        // Act
        var scheduledTask = recording.ToScheduledTask(ffmpegSettings);

        // Assert
        var command = scheduledTask.Command;
        Assert.Contains($"-i {channelUri}", command);
        Assert.Contains($"-t {Convert.ToInt32((endTime - startTime).TotalSeconds)}", command);
        Assert.Contains("-c:v copy", command);
        Assert.Contains("-c:a copy", command);
        Assert.Contains("-f mp4", command); // Default format if nothing specified
        Assert.Contains($"-metadata title=\"{name}\"", command);
        Assert.Contains($"-metadata description=\"{description}\"", command);
        Assert.Contains(filename, command);
    }

    [Fact]
    public void GenerateFfMpegCommandString_WithAllSettingsSpecified_AppliesAllSettings()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Test Recording";
        var description = "Test Description";
        var filename = "/media/test_recording.mkv";
        var channelUri = "http://example.com/stream1";
        var startTime = DateTimeOffset.Now;
        var endTime = startTime.AddHours(1);
        var recording = new ScheduledRecording(id, name, description, filename, channelUri, "Test Channel", startTime, endTime);

        // Create settings with all options specified
        var ffmpegSettings = new FfmpegSettings
        {
            Codec = "libx265",
            AudioCodec = "aac",
            VideoBitrate = "2000k",
            AudioBitrate = "192k",
            Resolution = "1920x1080",
            FrameRate = "60",
            AspectRatio = "16:9",
            OutputFormat = "mkv",
            FileType = "mkv"
        };

        // Act
        var scheduledTask = recording.ToScheduledTask(ffmpegSettings);

        // Assert
        var command = scheduledTask.Command;
        Assert.Contains($"-i {channelUri}", command);
        Assert.Contains($"-t {Convert.ToInt32((endTime - startTime).TotalSeconds)}", command);
        Assert.Contains("-c:v libx265", command);
        Assert.Contains("-c:a aac", command);
        Assert.Contains("-b:v 2000k", command);
        Assert.Contains("-b:a 192k", command);
        Assert.Contains("-s 1920x1080", command);
        Assert.Contains("-r 60", command);
        Assert.Contains("-aspect 16:9", command);
        Assert.Contains("-f mkv", command);
        Assert.Contains($"-metadata title=\"{name}\"", command);
        Assert.Contains($"-metadata description=\"{description}\"", command);
        Assert.Contains(filename, command);
    }

    [Fact]
    public void GenerateFfMpegCommandString_WithSomeSettingsSpecified_AppliesOnlySpecifiedSettings()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Test Recording";
        var description = "Test Description";
        var filename = "/media/test_recording.mp4";
        var channelUri = "http://example.com/stream1";
        var startTime = DateTimeOffset.Now;
        var endTime = startTime.AddHours(1);
        var recording = new ScheduledRecording(id, name, description, filename, channelUri, "Test Channel", startTime, endTime);

        // Create settings with only some options specified
        var ffmpegSettings = new FfmpegSettings
        {
            Codec = "libx264", // specified
            AudioCodec = "", // empty, should use copy
            VideoBitrate = "1000k", // specified
            AudioBitrate = "", // empty, should be omitted
            Resolution = "1280x720", // specified
            FrameRate = "", // empty, should be omitted
            AspectRatio = "", // empty, should be omitted
            OutputFormat = "mp4", // specified
            FileType = "mp4" // specified but redundant with OutputFormat
        };

        // Act
        var scheduledTask = recording.ToScheduledTask(ffmpegSettings);

        // Assert
        var command = scheduledTask.Command;
        Assert.Contains($"-i {channelUri}", command);
        Assert.Contains($"-t {Convert.ToInt32((endTime - startTime).TotalSeconds)}", command);
        Assert.Contains("-c:v libx264", command);
        Assert.Contains("-c:a copy", command);
        Assert.Contains("-b:v 1000k", command);
        Assert.DoesNotContain("-b:a", command);
        Assert.Contains("-s 1280x720", command);
        Assert.DoesNotContain("-r", command);
        Assert.DoesNotContain("-aspect", command);
        Assert.Contains("-f mp4", command);
        Assert.Contains($"-metadata title=\"{name}\"", command);
        Assert.Contains($"-metadata description=\"{description}\"", command);
        Assert.Contains(filename, command);
    }

    [Fact]
    public void GenerateFfMpegCommandString_OutputFormatFallsBackToFileType()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Test Recording";
        var description = "Test Description";
        var filename = "/media/test_recording.mp4";
        var channelUri = "http://example.com/stream1";
        var startTime = DateTimeOffset.Now;
        var endTime = startTime.AddHours(1);
        var recording = new ScheduledRecording(id, name, description, filename, channelUri, "Test Channel", startTime, endTime);

        // Create settings with OutputFormat empty but FileType specified
        var ffmpegSettings = new FfmpegSettings
        {
            Codec = "libx264",
            AudioCodec = "aac",
            OutputFormat = "", // Empty, should fall back to FileType
            FileType = "mkv" // Should be used as format
        };

        // Act
        var scheduledTask = recording.ToScheduledTask(ffmpegSettings);

        // Assert
        var command = scheduledTask.Command;
        Assert.Contains("-f mkv", command); // Should use FileType as fallback
    }
}