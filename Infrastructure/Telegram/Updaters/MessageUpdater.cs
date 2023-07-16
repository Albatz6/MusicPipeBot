using Microsoft.Extensions.Logging;
using MusicPipeBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MusicPipeBot.Infrastructure.Telegram.Updaters;

public class MessageUpdater : IMessageUpdater
{
    private readonly ITelegramBotClient _botClient;
    private readonly IPipeService _pipe;
    private readonly ILogger<MessageUpdater> _logger;

    public MessageUpdater(ITelegramBotClient botClient, IPipeService pipe, ILogger<MessageUpdater> logger)
    {
        _botClient = botClient;
        _pipe = pipe;
        _logger = logger;
    }

    public async Task ProcessMessageReceive(Message message, CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Received message from {user}. Message type: {MessageType}", message.From, message.Type);
        if (message.Text is not { } messageText)
            return;

        var sentMessage = messageText.Split(' ')[0] switch
        {
            "/start" => await SendStartingReply(message, stoppingToken),
            "/loadtrack" => await SendTrackFile(message, stoppingToken),
            _ => await ShowUsage(message, stoppingToken)
        };

        _logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);
    }

    private async Task<Message> SendStartingReply(Message message, CancellationToken stoppingToken) =>
        await SendTextMessage(
            message.Chat.Id, "Hi! You can get a track from Spotify or Youtube using this bot :)", stoppingToken);

    private async Task<Message> SendTrackFile(Message message, CancellationToken stoppingToken)
    {
        var url = GetUrlFromQuery(message.Text);
        if (url is null)
            return await SendTextMessage(message.Chat.Id, "Invalid URL", stoppingToken);

        _logger.LogInformation("Started track downloading & sending");
        await SendTextMessage(message.Chat.Id, "Downloading the track can take up to a minute, please wait :)", stoppingToken);
        var trackFile = _pipe.DownloadTrack(url);
        if (trackFile is null)
            return await SendTextMessage(message.Chat.Id, "Couldn't download the track. Check your URL", stoppingToken);

        await _botClient.SendChatActionAsync(message.Chat.Id, ChatAction.UploadDocument, cancellationToken: stoppingToken);
        var separatedPath = trackFile.Split(Path.DirectorySeparatorChar);
        var (downloadId, fileName) = (separatedPath[1], separatedPath.Last());

        Message? sentMessage;
        await using (FileStream fileStream = new(trackFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            sentMessage = await _botClient.SendAudioAsync(
                chatId: message.Chat.Id, audio: new InputFileStream(fileStream, fileName),
                cancellationToken: stoppingToken);

        _pipe.RemoveTemporaryDirectories(new[] { downloadId });
        return sentMessage;
    }

    private async Task<Message> ShowUsage(Message message, CancellationToken stoppingToken)
    {
        const string usage = "Usage:\n" +
                             "/loadtrack _Spotify or YTMusic URL_ — Get a track \n";

        return await SendMarkupTextMessage(message.Chat.Id, usage, stoppingToken);
    }

    private static string? GetUrlFromQuery(string? query)
    {
        if (query is null)
            return null;

        var keywords = query.Split(' ');
        if (keywords.Length <= 1)
            return null;

        return string.IsNullOrWhiteSpace(keywords[1]) ? null : keywords[1];
    }

    private async Task<Message> SendTextMessage(long chatId, string text, CancellationToken stoppingToken) =>
        await _botClient.SendTextMessageAsync(chatId, text, cancellationToken: stoppingToken);

    private async Task<Message> SendMarkupTextMessage(long chatId, string text, CancellationToken stoppingToken) =>
        await _botClient.SendTextMessageAsync(chatId, text, parseMode: ParseMode.MarkdownV2, cancellationToken: stoppingToken);
}