using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MusicPipeBot.Services.Telegram.Core;

public interface ISendingService
{
    Task<Message> SendTextMessage(ChatId chatId, string text, CancellationToken cancellationToken);

    Task<Message> SendMarkupTextMessage(ChatId chatId, string text, CancellationToken cancellationToken);

    Task SendChatAction(ChatId chatId, ChatAction chatAction, CancellationToken cancellationToken);

    Task<Message> SendAudio(ChatId chatId, InputFileStream fileStream, CancellationToken cancellationToken);
}

public class SendingService(ITelegramBotClient botClient, ILogger<SendingService> logger) : ISendingService
{
    public async Task<Message> SendTextMessage(ChatId chatId, string text, CancellationToken cancellationToken) =>
        await botClient.SendTextMessageAsync(chatId, text, cancellationToken: cancellationToken);

    public async Task<Message> SendMarkupTextMessage(ChatId chatId, string text, CancellationToken cancellationToken) =>
        await botClient.SendTextMessageAsync(chatId, text, parseMode: ParseMode.MarkdownV2, cancellationToken: cancellationToken);

    public async Task SendChatAction(ChatId chatId, ChatAction chatAction, CancellationToken cancellationToken) =>
        await botClient.SendChatActionAsync(chatId, chatAction, cancellationToken: cancellationToken);

    public async Task<Message> SendAudio(ChatId chatId, InputFileStream fileStream, CancellationToken cancellationToken) =>
        await botClient.SendAudioAsync(chatId, fileStream, cancellationToken: cancellationToken);
}