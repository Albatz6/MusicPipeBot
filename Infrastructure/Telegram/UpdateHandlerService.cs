using Microsoft.Extensions.Logging;
using MusicPipeBot.Services;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MusicPipeBot.Infrastructure.Telegram;

public class UpdateHandlerService : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IPipeService _pipe;
    private readonly ILogger<UpdateHandlerService> _logger;

    public UpdateHandlerService(ITelegramBotClient botClient, IPipeService pipe, ILogger<UpdateHandlerService> logger)
    {
        _botClient = botClient;
        _pipe = pipe;
        _logger = logger;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken stoppingToken)
    {
        var handler = update switch
        {
            { Message: { } message } => BotOnMessageReceived(message, stoppingToken),
            _ => UnknownUpdateHandlerAsync(update)
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

    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Receive message type: {MessageType}", message.Type);
        if (message.Text is not { } messageText)
            return;

        var action = messageText.Split(' ')[0] switch
        {
            "/start" => SendStartingReply(message, cancellationToken),
            "/loadtrack" => SendTrackFile(message, cancellationToken),
            _ => ShowUsage(message, cancellationToken)
        };

        var sentMessage = await action;
        _logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);
    }

    private async Task<Message> SendStartingReply(Message message, CancellationToken stoppingToken) =>
        await SendTextMessage(
            message, "Hi! You can get a track from Spotify or Youtube using this very bot :)", stoppingToken);

    private async Task<Message> SendTrackFile(Message message, CancellationToken stoppingToken)
    {
        var url = GetUrlFromQuery(message);
        if (url is null)
            return await SendTextMessage(message, "Invalid URL", stoppingToken);

        await SendTextMessage(message, "Downloading the track can take up to a minute, please wait :)", stoppingToken);
        var trackFile = _pipe.DownloadTrack(url);
        if (trackFile is null)
            return await SendTextMessage(message, "Couldn't download the track. Check your URL", stoppingToken);

        await _botClient.SendChatActionAsync(message.Chat.Id, ChatAction.UploadDocument, cancellationToken: stoppingToken);
        await using FileStream fileStream = new(trackFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        var fileName = trackFile.Split(Path.DirectorySeparatorChar).Last();

        var sentMessage = await _botClient.SendAudioAsync(
            chatId: message.Chat.Id, audio: new InputFileStream(fileStream, fileName), cancellationToken: stoppingToken);
        _pipe.RemoveTemporaryDirectories(new[] { trackFile.Split(Path.DirectorySeparatorChar).First() });

        return sentMessage;
    }

    private static string? GetUrlFromQuery(Message message)
    {
        if (message.Text is null)
            return null;

        var keywords = message.Text.Split(' ');
        if (keywords.Length <= 1)
            return null;

        return string.IsNullOrWhiteSpace(keywords[1]) ? null : keywords[1];
    }

    private async Task<Message> ShowUsage(Message message, CancellationToken stoppingToken)
    {
        const string usage = "Usage:\n" +
                             "/loadtrack _Spotify or YTMusic URL_ — Get a track \n";

        return await SendMarkupTextMessage(message, usage, stoppingToken);
    }

    private async Task<Message> SendTextMessage(Message message, string text, CancellationToken stoppingToken) =>
        await _botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: text, cancellationToken: stoppingToken);

    private async Task<Message> SendMarkupTextMessage(Message message, string text, CancellationToken stoppingToken) =>
        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id, text: text, parseMode: ParseMode.MarkdownV2, cancellationToken: stoppingToken);

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        _logger.LogInformation("Unknown or unsupported update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
}