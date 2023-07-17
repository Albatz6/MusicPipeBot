namespace MusicPipeBot.Infrastructure.Telegram.Core.Interfaces;

public interface IReceiverService
{
    Task ReceiveAsync(CancellationToken stoppingToken);
}