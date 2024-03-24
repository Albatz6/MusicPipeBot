using AqueductCommon.Extensions;
using MusicPipeBot.Helpers;
using MusicPipeBot.Models;
using MusicPipeBot.Services;
using MusicPipeBot.Services.Telegram.Core;
using MusicPipeBot.StateMachine.Contexts;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MusicPipeBot.StateMachine.States;

public class DownloadingState(
    ISendingService sendingService,
    IPipeService pipeService,
    ILogger<DownloadingState> logger) : IState
{
    public StateName CurrentStateName => StateName.Downloading;

    public async Task<StateExecutionResult> Execute(UserState userState, Update update, CancellationToken cancellationToken)
    {
        // TODO: Add attributes for checking non-callback and callback states
        var message = update.Message;
        if (message is null)
        {
            logger.Info("Update {uid} doesn't contain message, returning from the initial state", update.Id);
            return StateExecutionResult.GetSkipped(userState);
        }

        if (message.Text is null && message.Audio is null && message.Document is null)
        {
            logger.Info("Update {uid} contains message without any text, returning from the initial state", update.Id);
            var errorMessage = await sendingService.SendTextMessage(message.Chat.Id, "This message type is unsupported by the bot", cancellationToken);
            return StateExecutionResult.GetCompleted(userState, errorMessage);
        }

        var (queryValidationErrorMessage, url) = await GetQueryUrl(message, cancellationToken);
        if (queryValidationErrorMessage is not null)
            return StateExecutionResult.GetCompleted(StateName.Initial, queryValidationErrorMessage);

        var (downloadingErrorMessage, trackFile) = await DownloadAndGetTrackFilePath(message, url!, cancellationToken);
        if (downloadingErrorMessage is not null)
            return StateExecutionResult.GetCompleted(StateName.Initial, downloadingErrorMessage);

        var (sentMessage, downloadId) = await SendTrack(message, trackFile!, cancellationToken);

        _ = await Task.Run(() => pipeService.RemoveTemporaryDirectories(new[] { downloadId }), cancellationToken);
        return StateExecutionResult.GetTransitioned(
            StateName.UploadToYandex,
            new YandexUploadStateContext { DownloadId = downloadId },
            sentMessage);
    }

    private async Task<(Message? message, string? Url)> GetQueryUrl(Message message, CancellationToken cancellationToken)
    {
        var url = QueryValidationHelper.GetUrlFromQuery(message.Text);
        if (url is not null)
            return (null, url);

        logger.Warn("Invalid URL. Full user's message: '{url}'", message.Text);
        var sentMessage = await sendingService.SendTextMessage(
            message.Chat.Id,
            "Invalid URL. Check if you're trying to download a track, not an album or any other playlist",
            cancellationToken);
        return (sentMessage, null);
    }

    private async Task<(Message? message, string? filePath)> DownloadAndGetTrackFilePath(
        Message message, string url, CancellationToken cancellationToken)
    {
        logger.Info("Started track downloading & sending. Query: {query}", url);
        await sendingService.SendTextMessage(message.Chat.Id, "Downloading the track can take up to a minute, please wait :)", cancellationToken);

        var trackFile = pipeService.DownloadTrack(url);
        if (trackFile is not null)
            return (null, trackFile);

        return (
            await sendingService.SendTextMessage(message.Chat.Id, "Couldn't download the track. Check your URL", cancellationToken),
            null);
    }

    private async Task<(Message message, string downloadId)> SendTrack(Message message, string trackFile, CancellationToken cancellationToken)
    {
        await sendingService.SendChatAction(message.Chat.Id, ChatAction.UploadDocument, cancellationToken);
        var separatedPath = trackFile.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var (downloadId, fileName) = (separatedPath[1], separatedPath.Last());

        Message? sentMessage;
        await using (FileStream fileStream = new(trackFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            sentMessage = await sendingService.SendAudio(message.Chat.Id, new InputFileStream(fileStream, fileName), cancellationToken);

        return (sentMessage, downloadId);
    }
}