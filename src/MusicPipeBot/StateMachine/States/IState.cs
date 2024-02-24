using AqueductCommon.Results;
using MusicPipeBot.Models;
using Telegram.Bot.Types;

namespace MusicPipeBot.StateMachine.States;

public interface IState
{
    Task<Result<StateExecutionResult>> Execute(UserState userState, Update update, CancellationToken cancellationToken);
}