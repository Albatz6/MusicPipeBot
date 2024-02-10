namespace MusicPipeBot.Infrastructure.Settings;

public class AppSettings
{
    public const string SectionName = "Settings";

    public TelegramBotSettings TelegramBot { get; set; } = null!;
    public ApiSettings Api { get; set; } = null!;
    public RateLimitSettings RateLimit { get; set; } = null!;
    public HttpClientPolicySettings HttpClientPolicy { get; set; } = null!;
}