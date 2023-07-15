using Microsoft.Extensions.Logging;
using MusicPipeBot.Infrastructure.Telegram.Interfaces;

namespace MusicPipeBot.Infrastructure.Telegram;

public class PollingService : IPollingService
{
    private readonly IReceiverService _receiver;
    private readonly ILogger<PollingService> _logger;

    public PollingService(IReceiverService receiver, ILogger<PollingService> logger)
    {
        _receiver = receiver;
        _logger = logger;
    }

    public async Task PollAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _receiver.ReceiveAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Polling failed with exception: {exception}", ex);

            // Cooldown if something goes wrong
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }
}