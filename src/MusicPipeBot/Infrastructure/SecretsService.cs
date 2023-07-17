using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MusicPipeBot.Models;

namespace MusicPipeBot.Infrastructure;

public class SecretsService : ISecretsService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SecretsService> _log;

    public SecretsService(IConfigurationBuilder configBuilder, ILogger<SecretsService> log)
    {
        _configuration = configBuilder.Build();
        _log = log;
    }

    public TelegramBotCredentials? GetTelegramCredentials()
    {
        _log.LogInformation("Getting Telegram bot credentials...");
        return _configuration.GetSection("TelegramBotCredentials").Get<TelegramBotCredentials>();
    }
}