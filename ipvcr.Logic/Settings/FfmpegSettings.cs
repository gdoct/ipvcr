namespace ipvcr.Logic.Settings;
public class FfmpegSettings
{
    // class for managing ffmpeg parameters such as file type, codec, etc.
    public string FileType { get; set; } = "mp4";
    public string Codec { get; set; } = "";
    public string AudioCodec { get; set; } = "";
    public string VideoBitrate { get; set; } = "";
    public string AudioBitrate { get; set; } = "";
    public string Resolution { get; set; } = "";
    public string FrameRate { get; set; } = "";
    public string AspectRatio { get; set; } = "";
    public string OutputFormat { get; set; } = "mp4";
}
