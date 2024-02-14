﻿using AqueductCommon.Extensions;
using MusicPipeBot.Services.Telegram.Updaters;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace MusicPipeBot.Services.Telegram.Core;

public class UpdateHandler(IMessageUpdater messageUpdater, ILogger<UpdateHandler> logger) : IUpdateHandler
{
    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken stoppingToken)
    {
        var handler = update switch
        {
            { Message: { } message } => messageUpdater.ProcessMessageReceive(message, stoppingToken),
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

        logger.Error("Polling failure. Error: {errorMessage}", errorMessage);

        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
    }

    private Task HandleUnknownUpdate(Update update)
    {
        logger.Info("Unknown or unsupported update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
}