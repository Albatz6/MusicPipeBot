namespace MusicPipeBot.Infrastructure.Settings;

public class ApiSettings
{
    public const string SectionName = "Api";

    public string BaseUrl { get; set; } = null!;
    public string ApiKey { get; set; } = null!;
    public string AuthHeaderName { get; set; } = null!;
}