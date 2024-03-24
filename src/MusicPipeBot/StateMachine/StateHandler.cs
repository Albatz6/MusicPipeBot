using System.Net;
using Aqueduct.Client;
using Aqueduct.Client.Models;
using AqueductCommon.Extensions;
using AqueductCommon.Results;
using Microsoft.Kiota.Abstractions;
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
    AqueductClient aqueductClient,
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
            await sendingService.SendTextMessage(chatId, "Failed to get or add user data", stoppingToken);
            return;
        }
        var userState = userStateResult.Value;

        var initialHandlerExecutionResult =
            await serviceProvider.GetRequiredService<InitialState>().Execute(userState, update, stoppingToken);
        if (initialHandlerExecutionResult.Completed)
        {
            logger.Info("Executed initial state, skipping the rest");
            await UpdateUserState(userState, chatId, initialHandlerExecutionResult, stoppingToken);
            return;
        }

        await ExecuteNonInitialState(userState, update, user, chatId, stoppingToken);
    }

    private async Task ExecuteNonInitialState(UserState userState, Update update, User user, long chatId, CancellationToken cancellationToken)
    {
        var stateHandlerType = typeof(IState);
        var stateHandler = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsAssignableFrom(stateHandlerType))
            .Select(t => serviceProvider.GetRequiredService(t) as IState)
            .SingleOrDefault(s => s!.CurrentStateName == userState.CurrentState);

        if (stateHandler == default)
        {
            logger.Error("Failed to get state handler for user {tgId} with state {userState}", user, userState.CurrentState);
            await sendingService.SendTextMessage(chatId, "Failed to handle the message due to an internal error. Try later", cancellationToken);
            return;
        }

        logger.Info("Chosen {state} handler for user {tgId} with state {userState}", stateHandler.CurrentStateName, user, userState.CurrentState);
        var executionResult = await stateHandler.Execute(userState, update, cancellationToken);
        await UpdateUserState(userState, chatId, executionResult, cancellationToken);
    }

    private async Task<Result<UserState>> GetOrCreateRequestingUserState(long telegramUserId)
    {
        var existingUserStateResult = await userStatesRepository.Get(telegramUserId);
        if (existingUserStateResult.IsSuccess)
            return existingUserStateResult;

        var registeredUserResult = await RegisterUser(telegramUserId);
        if (!registeredUserResult.IsSuccess)
            return (registeredUserResult.Exception, registeredUserResult.StatusCode);

        // Kiota produces only nullable models atm. Issue on this topic: https://github.com/microsoft/kiota/issues/3911
        return await userStatesRepository.Add(telegramUserId, registeredUserResult.Value.Phrase!);
    }

    private async Task<Result<UserResponse>> RegisterUser(long telegramUserId)
    {
        try
        {
            var registrationResult = await aqueductClient.Users.PostAsync(new RegisterUserRequest
            {
                TelegramUserId = telegramUserId.ToString()
            });
            return registrationResult!;
        }
        catch (Exception e)
        {
            var apiException = e as ApiException;

            logger.Error("Failed to register new user with Telegram id {id}. Status code: {code}. Message: {message}",
                telegramUserId, apiException?.ResponseStatusCode, e.Message);

            return (e, HttpStatusCode.InternalServerError);
        }
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
}