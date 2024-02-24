using AqueductCommon.Extensions;
using MusicPipeBot.StateMachine;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace MusicPipeBot.Services.Telegram.Core;

public class UpdateHandler(
    IStateHandler stateHandler,
    ILogger<UpdateHandler> logger) : IUpdateHandler
{
    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken stoppingToken)
    {
        var action = update switch
        {
            { Message: not null } => stateHandler.ProcessUpdate(
                update, update.Message.From!, update.Message.Chat.Id, stoppingToken),
            { CallbackQuery: not null } => stateHandler.ProcessUpdate(
                update, update.CallbackQuery.From, update.CallbackQuery.Message!.Chat.Id, stoppingToken),
            _ => throw new ArgumentOutOfRangeException(nameof(update), update, "Unexpected update type")
        };

        await action;
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
}