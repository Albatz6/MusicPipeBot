using AqueductCommon.Extensions;
using AqueductCommon.Results;
using MusicPipeBot.Models;
using MusicPipeBot.Services;
using MusicPipeBot.Services.Telegram.Core;
using Telegram.Bot.Types;

namespace MusicPipeBot.StateMachine.States;

public class InitialState(
    ISendingService sendingService,
    IDownloadService downloadService,
    ILogger<InitialState> logger)
    : IState
{
    public async Task<Result<StateExecutionResult>> Execute(UserState userState, Update update, CancellationToken cancellationToken)
    {
        var message = update.Message;
        if (message is null)
        {
            logger.Info("Update {uid} doesn't contain message, returning from the initial state", update.Id);
            var executionResult = new StateExecutionResult { NextStateName = userState.Name };
            return executionResult;
        }

        if (message.Text is null)
        {
            logger.Info("Update {uid} contains message without any text, returning from the initial state", update.Id);
            var errorMessage = await sendingService.SendTextMessage(message.Chat.Id, "This message type is unsupported by the bot", cancellationToken);

            var executionResult = new StateExecutionResult { NextStateName = userState.Name, SentMessage = errorMessage };
            return executionResult;
        }

        return message.Text!.Split(' ')[0] switch
        {
            "/start" => await SendStartingReply(message, cancellationToken),
            "/loadtrack" => await SendTrackFile(message, cancellationToken),
            _ => await ShowUsage(message, cancellationToken)
        };
    }

    private async Task<StateExecutionResult> SendStartingReply(Message message, CancellationToken cancellationToken)
    {
        var sentMessage = await sendingService.SendTextMessage(
            message.Chat.Id, "Hi! You can get a track from Spotify or Youtube using this bot :)", cancellationToken);

        return new StateExecutionResult
        {
            NextStateName = StateName.Initial,
            SentMessage = sentMessage
        };
    }

    private async Task<StateExecutionResult> SendTrackFile(Message message, CancellationToken cancellationToken)
    {
        var (sentMessage, downloadId) = await downloadService.DownloadTrack(message, cancellationToken);
        if (downloadId is null)
            return new StateExecutionResult
            {
                NextStateName = StateName.Initial,
                SentMessage = sentMessage
            };

        return new StateExecutionResult
        {
            NextStateName = StateName.Initial,
            NextStateContext = new YandexUploadStateContext { DownloadId = downloadId },
            SentMessage = sentMessage
        };
    }
    private async Task<StateExecutionResult> ShowUsage(Message message, CancellationToken cancellationToken)
    {
        const string usage = "Usage:\n" +
                             "/loadtrack _Spotify or YTMusic URL_ — Get a track \n";

        var sentMessage = await sendingService.SendMarkupTextMessage(message.Chat.Id, usage, cancellationToken);

        return new StateExecutionResult
        {
            NextStateName = StateName.Initial,
            SentMessage = sentMessage
        };
    }
}