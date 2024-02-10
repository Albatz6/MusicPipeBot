namespace MusicPipeBot.Infrastructure.Settings;

public class HttpClientPolicySettings
{
    public const string SectionName = "HttpClientPolicy";

    public int RetriesCount { get; set; } = 3;
}