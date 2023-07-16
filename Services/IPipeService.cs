namespace MusicPipeBot.Services;

public interface IPipeService
{
    // string? GetTrackDownloadUrl(string query);
    string? DownloadTrack(string query);
    bool RemoveTemporaryDirectories(IEnumerable<string> directories);
}