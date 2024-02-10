using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MusicPipeBot.Services.Telegram.Updaters;

public interface IMessageUpdater
{
    Task ProcessMessageReceive(Message message, CancellationToken stoppingToken);
}

public class MessageUpdater(ITelegramBotClient botClient, IPipeService pipe, ILogger<MessageUpdater> logger) : IMessageUpdater
{
    public async Task ProcessMessageReceive(Message message, CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Received message from {user}. Message type: {MessageType}", message.From, message.Type);
        if (message.Text is not { } messageText)
            return;

        var sentMessage = messageText.Split(' ')[0] switch
        {
            "/start" => await SendStartingReply(message, stoppingToken),
            "/loadtrack" => await SendTrackFile(message, stoppingToken),
            _ => await ShowUsage(message, stoppingToken)
        };

        logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);
    }

    private async Task<Message> SendStartingReply(Message message, CancellationToken stoppingToken) =>
        await SendTextMessage(
            message.Chat.Id, "Hi! You can get a track from Spotify or Youtube using this bot :)", stoppingToken);

    private async Task<Message> SendTrackFile(Message message, CancellationToken stoppingToken)
    {
        var (errorMessage, trackFile) = await DownloadAndGetTrackFilePath(message, stoppingToken);
        if (errorMessage is not null)
            return errorMessage;

        await botClient.SendChatActionAsync(message.Chat.Id, ChatAction.UploadDocument, cancellationToken: stoppingToken);
        var separatedPath = trackFile!.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var (downloadId, fileName) = (separatedPath[1], separatedPath.Last());

        Message? sentMessage;
        await using (FileStream fileStream = new(trackFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            sentMessage = await botClient.SendAudioAsync(
                chatId: message.Chat.Id, audio: new InputFileStream(fileStream, fileName),
                cancellationToken: stoppingToken);

        pipe.RemoveTemporaryDirectories(new[] { downloadId });
        return sentMessage;
    }

    private async Task<(Message? Sent, string? Url)> DownloadAndGetTrackFilePath(Message message, CancellationToken stoppingToken)
    {
        var url = GetUrlFromQuery(message.Text);
        if (url is null)
        {
            logger.LogWarning("Invalid URL. Full user's message: '{url}'", message.Text);
            var sentMessage = await SendTextMessage(
                message.Chat.Id,
                "Invalid URL. Check if you're trying to download a track, not an album or any other playlist",
                stoppingToken);
            return (sentMessage, null);
        }

        logger.LogInformation("Started track downloading & sending. Query: {query}", url);
        await SendTextMessage(message.Chat.Id, "Downloading the track can take up to a minute, please wait :)", stoppingToken);

        var trackFile = pipe.DownloadTrack(url);
        if (trackFile is not null)
            return (null, trackFile);

        return (await SendTextMessage(message.Chat.Id, "Couldn't download the track. Check your URL", stoppingToken),
                null);
    }

    private async Task<Message> ShowUsage(Message message, CancellationToken stoppingToken)
    {
        const string usage = "Usage:\n" +
                             "/loadtrack _Spotify or YTMusic URL_ — Get a track \n";

        return await SendMarkupTextMessage(message.Chat.Id, usage, stoppingToken);
    }

    private static string? GetUrlFromQuery(string? query)
    {
        var validatedQuery = ValidateQuery(query);
        if (validatedQuery == default)
            return null;

        if (validatedQuery.Contains("youtu.be"))
            return GetYoutubeUrl(validatedQuery);

        // These are the markers of track link for Spotify and YTMusic
        if (!validatedQuery.Contains("track") && !validatedQuery.Contains("watch"))
            return null;

        var isValidUri = Uri.TryCreate(validatedQuery, UriKind.Absolute, out var uriResult)
                       && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        return !isValidUri ? null : validatedQuery;
    }

    private static string? ValidateQuery(string? query)
    {
        if (query is null)
            return null;

        // Separate by ; and & in case somebody tries injecting other commands into the query
        var keywords = query.Split(' ', ';', '&');
        if (keywords.Length <= 1)
            return null;

        return string.IsNullOrWhiteSpace(keywords[1]) ? null : keywords[1];
    }

    private static string? GetYoutubeUrl(string validatedQuery)
    {
        var ytHashWithQueryParams = validatedQuery.Split('/').LastOrDefault();
        if (ytHashWithQueryParams == default)
            return null;

        // Avoiding all those analytical query params like "si"
        var ytHash = ytHashWithQueryParams.Split('?').FirstOrDefault();
        return ytHash == default ? null : $"https://music.youtube.com/watch?v={ytHash}";
    }

    private async Task<Message> SendTextMessage(long chatId, string text, CancellationToken stoppingToken) =>
        await botClient.SendTextMessageAsync(chatId, text, cancellationToken: stoppingToken);

    private async Task<Message> SendMarkupTextMessage(long chatId, string text, CancellationToken stoppingToken) =>
        await botClient.SendTextMessageAsync(chatId, text, parseMode: ParseMode.MarkdownV2, cancellationToken: stoppingToken);
}