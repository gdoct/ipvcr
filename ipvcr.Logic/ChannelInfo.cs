namespace ipvcr.Logic;

public class ChannelInfo(string id, string name, string logo, Uri uri, string group)
{
    public string Id { get; set; } = id;
    public string Name { get; set; } = name;
    public string Logo { get; set; } = logo;
    public Uri Uri { get; set; } = uri;
    public string Group { get; set; } = group;
}