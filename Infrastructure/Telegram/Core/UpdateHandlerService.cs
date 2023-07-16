using Microsoft.Extensions.Logging;
using MusicPipeBot.Infrastructure.Telegram.Updaters;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace MusicPipeBot.Infrastructure.Telegram.Core;

public class UpdateHandlerService : IUpdateHandler
{
    private readonly IMessageUpdater _messageUpdater;
    private readonly ILogger<UpdateHandlerService> _logger;

    public UpdateHandlerService(IMessageUpdater messageUpdater, ILogger<UpdateHandlerService> logger)
    {
        _messageUpdater = messageUpdater;
        _logger = logger;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken stoppingToken)
    {
        var handler = update switch
        {
            { Message: { } message } => _messageUpdater.ProcessMessageReceive(message, stoppingToken),
            _ => HandleUnknownUpdate(update)
        };

        await handler;
    }

    public async Task HandlePollingErrorAsync(ITelegramBotClient _, Exception exception, CancellationToken stoppingToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException =>
                $"Telegram API Exception.\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("Polling failure. Error: {errorMessage}", errorMessage);

        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
    }

    private Task HandleUnknownUpdate(Update update)
    {
        _logger.LogInformation("Unknown or unsupported update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
}