using Telegram.Bot.Types;

namespace MusicPipeBot.Infrastructure.Telegram.Updaters;

public interface IMessageUpdater
{
    Task ProcessMessageReceive(Message message, CancellationToken stoppingToken);
}