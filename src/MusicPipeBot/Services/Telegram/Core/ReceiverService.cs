using AqueductCommon.Extensions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace MusicPipeBot.Services.Telegram.Core;

public interface IReceiverService
{
    Task ReceiveAsync(CancellationToken cancellationToken);
}

public class ReceiverService(
    ITelegramBotClient botClient,
    IUpdateHandler updateHandler,
    ILogger<ReceiverService> logger)
    : IReceiverService
{
    /// <summary>
    /// Start to service Updates with UpdateHandler
    /// </summary>
    public async Task ReceiveAsync(CancellationToken cancellationToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery],
            ThrowPendingUpdates = true
        };

        var bot = await botClient.GetMeAsync(cancellationToken);
        logger.Info("Start receiving updates for {botName}", bot.Username ?? "MusicPipeBot");

        await botClient.ReceiveAsync(
            updateHandler: updateHandler, receiverOptions: receiverOptions, cancellationToken: cancellationToken);
    }
}