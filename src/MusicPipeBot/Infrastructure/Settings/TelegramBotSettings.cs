namespace MusicPipeBot.Infrastructure.Settings;

public class TelegramBotSettings
{
    public const string SectionName = "TelegramBot";

    public string Token { get; init; } = null!;
}