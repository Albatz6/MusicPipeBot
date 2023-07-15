namespace MusicPipeBot.Infrastructure.Telegram.Interfaces;

public interface IReceiverService
{
    Task ReceiveAsync(CancellationToken stoppingToken);
}