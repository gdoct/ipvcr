using ipvcr.Logic.Scheduler;
using ipvcr.Logic.Settings;

namespace ipvcr.Logic;

public class ScheduledRecording
{
    public string Description { get; set; } = string.Empty;
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
    public string ChannelUri { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public DateTimeOffset StartTime { get; set; } = DateTimeOffset.Now.AddDays(1);
    public DateTimeOffset EndTime { get; set; } = DateTimeOffset.Now.AddDays(1).AddHours(1);
    public FfmpegSettings? FfmpegSettings { get; set; }
    public ScheduledRecording()
    {

    }

    public ScheduledRecording(Guid id, string name, string description, string filename, string channelUri, string channelName, DateTimeOffset startTime, DateTimeOffset endTime, FfmpegSettings? ffmpegSettings = null)
    {
        Id = id;
        Name = name;
        Description = description;
        Filename = filename;
        ChannelUri = channelUri;
        ChannelName = channelName;
        StartTime = startTime;
        EndTime = endTime;
        FfmpegSettings = ffmpegSettings;
    }

    private string GenerateFfMpegCommandString(FfmpegSettings ffmpegSettings)
    {
        var command = $"ffmpeg -i {ChannelUri} -t {Convert.ToInt32((EndTime - StartTime).TotalSeconds)}";

        // Add video codec if specified
        if (!string.IsNullOrEmpty(ffmpegSettings.Codec))
        {
            command += $" -c:v {ffmpegSettings.Codec}";
        }
        else
        {
            command += " -c:v copy";
        }

        // Add audio codec if specified
        if (!string.IsNullOrEmpty(ffmpegSettings.AudioCodec))
        {
            command += $" -c:a {ffmpegSettings.AudioCodec}";
        }
        else
        {
            command += " -c:a copy";
        }

        // Add video bitrate if specified
        if (!string.IsNullOrEmpty(ffmpegSettings.VideoBitrate))
        {
            command += $" -b:v {ffmpegSettings.VideoBitrate}";
        }

        // Add audio bitrate if specified
        if (!string.IsNullOrEmpty(ffmpegSettings.AudioBitrate))
        {
            command += $" -b:a {ffmpegSettings.AudioBitrate}";
        }

        // Add resolution if specified
        if (!string.IsNullOrEmpty(ffmpegSettings.Resolution))
        {
            command += $" -s {ffmpegSettings.Resolution}";
        }

        // Add frame rate if specified
        if (!string.IsNullOrEmpty(ffmpegSettings.FrameRate))
        {
            command += $" -r {ffmpegSettings.FrameRate}";
        }

        // Add aspect ratio if specified
        if (!string.IsNullOrEmpty(ffmpegSettings.AspectRatio))
        {
            command += $" -aspect {ffmpegSettings.AspectRatio}";
        }

        // Use specified output format, falling back to FileType if needed
        string format = !string.IsNullOrEmpty(ffmpegSettings.OutputFormat)
            ? ffmpegSettings.OutputFormat
            : !string.IsNullOrEmpty(ffmpegSettings.FileType) ? ffmpegSettings.FileType : "mp4";

        command += $" -f {format}";

        // Add metadata
        command += $" -metadata title=\"{Name}\" -metadata description=\"{Description}\"";

        // Add filename
        command += $" {Filename}";

        return command;
    }

    public ScheduledTask ToScheduledTask(FfmpegSettings defaultFfmpegSettings)
    {
        // Use the recording's specific settings if available, otherwise fall back to default settings
        var settingsToUse = FfmpegSettings ?? defaultFfmpegSettings;
        
        return new(Id,
            Name,
            GenerateFfMpegCommandString(settingsToUse),
            StartTime,
            System.Text.Json.JsonSerializer.Serialize(this)
            );
    }

    public static ScheduledRecording FromScheduledTask(ScheduledTask scheduledTask)
    {
        var json = scheduledTask.InnerScheduledTask;
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new Exception("ScheduledTask.InnerScheduledTask is null or empty");
        }
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<ScheduledRecording>(json)!;
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new Exception($"Failed to deserialize ScheduledRecording from ScheduledTask: {ex.Message}", ex);
        }
    }

    public string Obfuscate()
    {
        // input uri is "http://secret.host.tv/username/password/219885"
        // transform to "http://secr.../219885"
        var parts = ChannelUri.Split('/');
        var first4letterersofhostname = parts[2].Substring(0, 4);
        var lastpart = parts[parts.Length - 1];
        var obfuscatedUri = parts[0] + "//" + first4letterersofhostname + "..." + "/" + lastpart;
        return obfuscatedUri;
    }
}
