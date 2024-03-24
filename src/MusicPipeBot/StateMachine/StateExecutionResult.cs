using MusicPipeBot.Models;
using MusicPipeBot.StateMachine.Contexts;
using Telegram.Bot.Types;

namespace MusicPipeBot.StateMachine;

public class StateExecutionResult
{
    public static StateExecutionResult GetSkipped(UserState userState) =>
        new()
        {
            Completed = false,
            NextStateName = userState.CurrentState
        };

    public static StateExecutionResult GetCompleted(UserState userState, Message sentMessage) =>
        new()
        {
            Completed = true,
            NextStateName = userState.CurrentState,
            SentMessage = sentMessage
        };

    public static StateExecutionResult GetCompleted(StateName stateName, Message sentMessage) =>
        new()
        {
            Completed = true,
            NextStateName = stateName,
            SentMessage = sentMessage
        };

    public static StateExecutionResult GetTransitioned(StateName stateName, IStateContext stateContext, Message sentMessage) =>
        new()
        {
            Completed = true,
            NextStateName = stateName,
            NextStateContext = stateContext,
            SentMessage = sentMessage
        };

    public required bool Completed { get; set; }

    public required StateName NextStateName { get; set; }

    public IStateContext? NextStateContext { get; set; }

    public Message? SentMessage { get; set; }
}