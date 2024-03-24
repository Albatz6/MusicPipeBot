using MusicPipeBot.Models;
using Telegram.Bot.Types;

namespace MusicPipeBot.StateMachine;

public class StateExecutionResult
{
    public static StateExecutionResult GetSkipped(UserState userState) =>
        new()
        {
            Completed = false,
            NextStateName = userState.Name
        };

    public static StateExecutionResult GetCompleted(UserState userState, Message sentMessage) =>
        new()
        {
            Completed = true,
            NextStateName = userState.Name,
            SentMessage = sentMessage
        };

    public static StateExecutionResult GetCompleted(StateName stateName, Message sentMessage) =>
        new()
        {
            Completed = true,
            NextStateName = stateName,
            SentMessage = sentMessage
        };

    public static StateExecutionResult GetTransitioned(StateName stateName, StateContext stateContext, Message sentMessage) =>
        new()
        {
            Completed = true,
            NextStateName = stateName,
            NextStateContext = stateContext,
            SentMessage = sentMessage
        };

    public required bool Completed { get; set; }

    public required StateName NextStateName { get; set; }

    public StateContext? NextStateContext { get; set; }

    public Message? SentMessage { get; set; }
}