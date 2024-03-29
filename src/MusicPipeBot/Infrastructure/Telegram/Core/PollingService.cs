﻿using Microsoft.Extensions.Logging;

namespace MusicPipeBot.Infrastructure.Telegram.Core;

public interface IPollingService
{
    Task PollAsync(CancellationToken stoppingToken);
}

public class PollingService(IReceiverService receiver, ILogger<PollingService> logger) : IPollingService
{
    public async Task PollAsync(CancellationToken stoppingToken)
    {
        try
        {
            await receiver.ReceiveAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError("Polling failed with exception: {exception}", ex);

            // Cooldown if something goes wrong
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }
}