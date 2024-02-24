using AqueductCommon.Extensions;
using AqueductCommon.Results;
using MusicPipeBot.Models;
using MusicPipeBot.Repositories;
using MusicPipeBot.Services.Telegram.Core;
using MusicPipeBot.StateMachine.States;
using Telegram.Bot.Types;

namespace MusicPipeBot.StateMachine;

public interface IStateHandler
{
    Task ProcessUpdate(Update update, User user, long chatId, CancellationToken stoppingToken);
}

public class StateHandler(
    ISendingService sendingService,
    IServiceProvider serviceProvider,
    IUserStatesRepository userStatesRepository,
    ILogger<StateHandler> logger) : IStateHandler
{
    public async Task ProcessUpdate(Update update, User user, long chatId, CancellationToken stoppingToken)
    {
        var userStateResult = await GetOrCreateRequestingUserState(user.Id);
        if (!userStateResult.IsSuccess)
        {
            await sendingService.SendTextMessage(chatId, "Failed to get or add user state", stoppingToken);
            return;
        }
        var userState = userStateResult.Value;

        // Another option is to have a base state handler that always runs before executing current state handler
        // In this case base state handler could be InitialState
        var executionResult = update.Message is null
            ? await HandleCallbackUpdate(update, user, userState, stoppingToken)
            : await HandleMessageUpdate(update, user, userState, stoppingToken);
        if (!executionResult.IsSuccess)
        {
            await sendingService.SendTextMessage(chatId, "Failed to execute user state", stoppingToken);
            return;
        }

        await UpdateUserState(userState, chatId, executionResult.Value!, stoppingToken);
    }

    private async Task<Result<UserState>> GetOrCreateRequestingUserState(long telegramUserId)
    {
        var existingUserResult = await userStatesRepository.Get(telegramUserId);
        if (existingUserResult.IsSuccess)
            return existingUserResult;

        return await userStatesRepository.Add(telegramUserId);
    }

    private async Task<Result<StateExecutionResult>> HandleCallbackUpdate(
        Update update, User user, UserState userState, CancellationToken stoppingToken)
    {
        var stateActionTask = GetStateActionTask(update, user, userState, stoppingToken);
        return await stateActionTask;
    }

    private Task<Result<StateExecutionResult>> GetStateActionTask(
        Update update, User user, UserState userState, CancellationToken stoppingToken) =>
        userState.Name switch
        {
            StateName.Initial => serviceProvider.GetRequiredService<InitialState>().Execute(userState, update, stoppingToken),
            // StateName.UploadToYandex => serviceProvider.GetService<YandexUploadState>()?.Execute(userState, update, stoppingToken)!,
            _ => HandleNonexistentState(userState.Name, user.Username!, stoppingToken)
        };

    private async Task<Result<StateExecutionResult>> HandleNonexistentState(
        StateName currentStateName, string telegramUsername, CancellationToken cancellationToken)
    {
        var errorMessage = await sendingService.SendTextMessage(
            telegramUsername,
            "Error occurred on state handling. Try your request later.",
            cancellationToken);

        logger.Error("Nonexistent state {state} for user {tgUsername}", currentStateName, telegramUsername);

        return new StateExecutionResult
        {
            NextStateName = StateName.Initial,
            SentMessage = errorMessage
        };
    }

    private async Task UpdateUserState(UserState userState, long chatId, StateExecutionResult executionResult, CancellationToken stoppingToken)
    {
        var userStateUpdatingResult = await userStatesRepository.Update(userState, executionResult.NextStateName, executionResult.NextStateContext);
        if (!userStateUpdatingResult.IsSuccess)
        {
            logger.Error("Error occurred while updating user {telegramId} state. Error: {error}",
                userState.TelegramId, userStateUpdatingResult.Exception.Message);
            await sendingService.SendTextMessage(chatId, "Unexpected error occurred on state updating. Try later", stoppingToken);
        }
    }

    private async Task<Result<StateExecutionResult>> HandleMessageUpdate(Update update, User user, UserState userState, CancellationToken stoppingToken)
    {
        var initialStateExecutionResult = await serviceProvider.GetRequiredService<InitialState>().Execute(userState, update, stoppingToken);
        if (!initialStateExecutionResult.IsSuccess || initialStateExecutionResult.Value.SentMessage != null)
            return initialStateExecutionResult;

        var currentStateAction = GetStateActionTask(update, user, userState, stoppingToken);
        return await currentStateAction;
    }
}