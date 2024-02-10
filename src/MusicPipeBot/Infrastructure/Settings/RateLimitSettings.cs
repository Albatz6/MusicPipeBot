namespace MusicPipeBot.Infrastructure.Settings;

public class RateLimitSettings
{
    public const string SectionName = "RateLimit";

    public int PermitLimit { get; set; } = 200;
    public int WindowInSeconds { get; set; } = 60;
    public int QueueLimit { get; set; } = 0;
}