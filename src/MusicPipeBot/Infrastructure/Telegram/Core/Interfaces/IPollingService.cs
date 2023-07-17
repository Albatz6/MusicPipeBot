namespace MusicPipeBot.Infrastructure.Telegram.Core.Interfaces;

public interface IPollingService
{
    Task PollAsync(CancellationToken stoppingToken);
}