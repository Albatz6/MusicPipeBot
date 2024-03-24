using MusicPipeBot.Models;
using Telegram.Bot.Types;

namespace MusicPipeBot.StateMachine.States;

public interface IState
{
    public StateName CurrentStateName { get; }

    Task<StateExecutionResult> Execute(UserState userState, Update update, CancellationToken cancellationToken);
}