using Microsoft.Extensions.Logging;
using MusicPipeBot.Infrastructure.Telegram.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace MusicPipeBot.Infrastructure.Telegram;

public class ReceiverService : IReceiverService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IUpdateHandler _updateHandler;
    private readonly ILogger<ReceiverService> _logger;

    public ReceiverService(
        ITelegramBotClient botClient, IUpdateHandler updateHandler, ILogger<ReceiverService> logger)
    {
        _botClient = botClient;
        _updateHandler = updateHandler;
        _logger = logger;
    }

    /// <summary>
    /// Start to service Updates with UpdateHandler
    /// </summary>
    /// <param name="stoppingToken"></param>
    public async Task ReceiveAsync(CancellationToken stoppingToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>(),
            ThrowPendingUpdates = true
        };

        var bot = await _botClient.GetMeAsync(stoppingToken);
        _logger.LogInformation("Start receiving updates for {botName}", bot.Username ?? "MusicPipeBot");

        await _botClient.ReceiveAsync(
            updateHandler: _updateHandler, receiverOptions: receiverOptions, cancellationToken: stoppingToken);
    }
}