﻿using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace MusicPipeBot.Infrastructure.Telegram.Core;

public interface IReceiverService
{
    Task ReceiveAsync(CancellationToken stoppingToken);
}

public class ReceiverService(
    ITelegramBotClient botClient,
    IUpdateHandler updateHandler,
    ILogger<ReceiverService> logger)
    : IReceiverService
{
    /// <summary>
    /// Start to service Updates with UpdateHandlerService
    /// </summary>
    public async Task ReceiveAsync(CancellationToken stoppingToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>(),
            ThrowPendingUpdates = true
        };

        var bot = await botClient.GetMeAsync(stoppingToken);
        logger.LogInformation("Start receiving updates for {botName}", bot.Username ?? "MusicPipeBot");

        await botClient.ReceiveAsync(
            updateHandler: updateHandler, receiverOptions: receiverOptions, cancellationToken: stoppingToken);
    }
}