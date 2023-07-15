using MusicPipeBot.Models;

namespace MusicPipeBot.Infrastructure;

public interface ISecretsService
{
    TelegramBotCredentials? GetTelegramCredentials();
}