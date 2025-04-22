namespace ipvcr.Logic.Settings;

public class PlaylistSettings
{
    public string M3uPlaylistPath { get; set; } = "/data/m3u-playlist.m3u";
    public int PlaylistAutoUpdateInterval { get; set; } = 24; // hours
    public bool AutoReloadPlaylist { get; set; } = false;
    public bool FilterEmptyGroups { get; set; } = true;
}