namespace MusicPipeBot.Infrastructure.Settings;

public class ApiSettings
{
    public const string SectionName = "Api";

    public string BaseAddress { get; set; } = null!;
    public string ApiKey { get; set; } = null!;
}