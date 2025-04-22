namespace ipvcr.Logic.Api;

using ipvcr.Logic;
using System.Collections.Generic;

public interface IPlaylistManager
{
    List<ChannelInfo> GetPlaylistItems();
    int ChannelCount { get; }
    Task LoadFromFileAsync(string filePath);
}
