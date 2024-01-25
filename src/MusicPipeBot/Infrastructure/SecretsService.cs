using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MusicPipeBot.Models;

namespace MusicPipeBot.Infrastructure;

public interface ISecretsService
{
    TelegramBotCredentials? GetTelegramCredentials();
}

public class SecretsService(IConfigurationBuilder configBuilder, ILogger<SecretsService> log) : ISecretsService
{
    private readonly IConfiguration _configuration = configBuilder.Build();

    public TelegramBotCredentials? GetTelegramCredentials()
    {
        log.LogInformation("Getting Telegram bot credentials...");
        return _configuration.GetSection("TelegramBotCredentials").Get<TelegramBotCredentials>();
    }
}