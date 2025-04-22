using ipvcr.Logic;

namespace ipvcr.Web.Models;
public class HomeRecordingsViewModel
{
    public string RecordingPath { get; set; } = "";
    public List<ScheduledRecording> Recordings { get; set; } = [];
    public List<ChannelInfo> Channels { get; set; } = [];
}