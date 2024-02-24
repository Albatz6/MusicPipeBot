using AqueductCommon.Extensions;
using AqueductCommon.Results;
using Microsoft.EntityFrameworkCore;
using MusicPipeBot.DbContexts;
using MusicPipeBot.Extensions;
using MusicPipeBot.Models;
using MusicPipeBot.StateMachine;

namespace MusicPipeBot.Repositories;

public interface IUserStatesRepository
{
    Task<Result<UserState>> Add(long telegramUserId);
    Task<Result<UserState>> Get(long telegramUserId);
    Task<Result<UserState>> Update(UserState currentState, StateName newStateName, StateContext? newStateContext);
}

public class UserStatesRepository(MainDbContext dbContext, ILogger<UserStatesRepository> logger) : IUserStatesRepository
{
    public async Task<Result<UserState>> Add(long telegramUserId)
    {
        try
        {
            logger.Info("Creating new state for user {telegramId}", telegramUserId);

            var user = new UserState
            {
                TelegramId = telegramUserId,
                Name = StateName.Initial
            };
            var result = await dbContext.UserStates.AddAsync(user);
            await dbContext.SaveChangesAsync();

            logger.Info("Successfully added new user {userId} with tgId {tgId}",
                result.Entity.Id, result.Entity.TelegramId);
            return result.Entity;
        }
        catch (Exception e)
        {
            logger.Error("An error occurred while creating new state for user {telegramId}. Error: {error}",
                telegramUserId, e.Message);
            return e;
        }
    }

    public async Task<Result<UserState>> Get(long telegramUserId)
    {
        logger.Info("Getting state for user {telegramId}", telegramUserId);

        var user = await dbContext.UserStates.SingleOrDefaultAsync(u => u.TelegramId == telegramUserId);

        if (user == default) return new KeyNotFoundException();
        return user;
    }

    public async Task<Result<UserState>> Update(UserState currentState, StateName newStateName, StateContext? newStateContext)
    {
        try
        {
            logger.Info("Updating state for user {telegramId}", currentState.TelegramId);

            currentState.Name = newStateName;
            currentState.Context = newStateContext is not null ? await newStateContext.GetJson() : currentState.Context;
            await dbContext.SaveChangesAsync();

            logger.Info("Successfully updated state for user {telegramId}", currentState.TelegramId);
            return currentState;
        }
        catch (Exception e)
        {
            logger.Error("Failed to update state for user {telegramId}. Error: {error}",
                currentState.TelegramId, e.Message);
            return e;
        }
    }
}