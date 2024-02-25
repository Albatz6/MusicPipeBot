using AqueductCommon.Extensions;
using MusicPipeBot.Services.Telegram.Core;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MusicPipeBot.Services;

public interface IDownloadService
{
    Task<(Message Message, string? downloadId)> DownloadTrack(Message message, CancellationToken cancellationToken);
}

public class DownloadService(ISendingService sendingService, IPipeService pipeService, ILogger<DownloadService> logger)
    : IDownloadService
{
    public async Task<(Message Message, string? downloadId)> DownloadTrack(Message message, CancellationToken cancellationToken)
    {
        var (errorMessage, trackFile) = await DownloadAndGetTrackFilePath(message, cancellationToken);
        if (errorMessage is not null)
            return (errorMessage, null);

        await sendingService.SendChatAction(message.Chat.Id, ChatAction.UploadDocument, cancellationToken);
        var separatedPath = trackFile!.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var (downloadId, fileName) = (separatedPath[1], separatedPath.Last());

        Message? sentMessage;
        await using (FileStream fileStream = new(trackFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            sentMessage = await sendingService.SendAudio(message.Chat.Id, new InputFileStream(fileStream, fileName), cancellationToken);

        _ = await Task.Run(() => pipeService.RemoveTemporaryDirectories(new[] { downloadId }), cancellationToken);
        return (sentMessage, downloadId);
    }

    private async Task<(Message? Sent, string? Url)> DownloadAndGetTrackFilePath(Message message, CancellationToken cancellationToken)
    {
        var url = GetUrlFromQuery(message.Text);
        if (url is null)
        {
            logger.Warn("Invalid URL. Full user's message: '{url}'", message.Text);
            var sentMessage = await sendingService.SendTextMessage(
                message.Chat.Id,
                "Invalid URL. Check if you're trying to download a track, not an album or any other playlist",
                cancellationToken);
            return (sentMessage, null);
        }

        logger.Info("Started track downloading & sending. Query: {query}", url);
        await sendingService.SendTextMessage(message.Chat.Id, "Downloading the track can take up to a minute, please wait :)", cancellationToken);

        var trackFile = pipeService.DownloadTrack(url);
        if (trackFile is not null)
            return (null, trackFile);

        return (
            await sendingService.SendTextMessage(message.Chat.Id, "Couldn't download the track. Check your URL", cancellationToken),
            null);
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
}