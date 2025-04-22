using System.IO.Abstractions;
using System.Text.RegularExpressions;

namespace ipvcr.Logic.Scheduler;

public class M3uParser
{
    private readonly IFileSystem _fileSystem;
    private readonly string _path;

    public M3uParser(IFileSystem fileSystem, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentNullException(nameof(path));

        ArgumentNullException.ThrowIfNull(fileSystem, nameof(fileSystem));

        if (!fileSystem.File.Exists(path))
            throw new FileNotFoundException("m3u file does not exist", path);

        _fileSystem = fileSystem;
        _path = path;
    }

    public M3uParser(string path)
        : this(new FileSystem(), path)
    {
    }

    public async IAsyncEnumerable<ChannelInfo> ParsePlaylistAsync()
    {
        if (string.IsNullOrWhiteSpace(_path) || !_fileSystem.File.Exists(_path)) yield break;
        using var stream = _fileSystem.File.OpenRead(_path);
        using var reader = new StreamReader(stream);
        string id = string.Empty;
        string name = string.Empty;
        string logo = string.Empty;
        string group = string.Empty;

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (TryParseGroup(line, out string groupname))
            {
                group = groupname;
            }
            if (line.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
            {
                var (tvgId, tvgName, tvgLogo, groupTitle) = ExtractAttributes(line);
                if (!string.IsNullOrWhiteSpace(groupTitle))
                {
                    group = groupTitle;
                }
                id = tvgId;
                name = tvgName;
                logo = tvgLogo;
            }
            else if (!string.IsNullOrWhiteSpace(id) &&
                Uri.IsWellFormedUriString(line, UriKind.Absolute))
            {
                var channel = new ChannelInfo(id, name, logo, new Uri(line), group);
                yield return channel;
                id = string.Empty;
            }
        }
    }

    private static bool TryParseGroup(string line, out string name)
    {
        const string marker = "#####";
        if (line.StartsWith(marker) && line.EndsWith(marker))
        {
            name = line[marker.Length..^marker.Length].Trim();
            return true;
        }
        name = string.Empty;
        return false;
    }

    private static (string tvgId, string tvgName, string tvgLogo, string groupTitle) ExtractAttributes(string input)
    {
        string tvgIdPattern = @"tvg-id=""(.*?)""";
        string tvgNamePattern = @"tvg-name=""(.*?)""";
        string tvgLogoPattern = @"tvg-logo=""(.*?)""";
        string groupTitlePattern = @"group-title=""(.*?)""";

        // Extract the values
        string tvgId = Regex.Match(input, tvgIdPattern).Groups[1].Value;
        string tvgName = Regex.Match(input, tvgNamePattern).Groups[1].Value;
        string tvgLogo = Regex.Match(input, tvgLogoPattern).Groups[1].Value;
        string groupTitle = Regex.Match(input, groupTitlePattern).Groups[1].Value;

        return (tvgId, tvgName, tvgLogo, groupTitle);
    }
}