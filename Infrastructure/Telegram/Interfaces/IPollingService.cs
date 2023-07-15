namespace MusicPipeBot.Infrastructure.Telegram.Interfaces;

public interface IPollingService
{
    Task PollAsync(CancellationToken stoppingToken);
}