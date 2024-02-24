using MusicPipeBot.Models;
using Telegram.Bot.Types;

namespace MusicPipeBot.StateMachine;

public class StateExecutionResult
{
    public required StateName NextStateName { get; set; }
    public StateContext? NextStateContext { get; set; }
    public Message? SentMessage { get; set; }
}