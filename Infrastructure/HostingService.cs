using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MusicPipeBot.Infrastructure.Telegram.Interfaces;

namespace MusicPipeBot.Infrastructure;

public class HostingService : BackgroundService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IPollingService _pollingService;
    private readonly ILogger<HostingService> _logger;

    public HostingService(
        IHostApplicationLifetime lifetime, IPollingService pollingService, ILogger<HostingService> logger)
    {
        _lifetime = lifetime;
        _pollingService = pollingService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HostingService is starting...");
        if (!await WaitForAppStartup(_lifetime, stoppingToken))
            return;

        while (!stoppingToken.IsCancellationRequested)
            await _pollingService.PollAsync(stoppingToken);
    }

    private static async Task<bool> WaitForAppStartup(IHostApplicationLifetime lifetime, CancellationToken stoppingToken)
    {
        var startedSource = new TaskCompletionSource();
        await using var applicationStartedToken = lifetime.ApplicationStarted.Register(() => startedSource.SetResult());

        var cancelledSource = new TaskCompletionSource();
        await using var cancellationToken = stoppingToken.Register(() => cancelledSource.SetResult());

        var completedTask = await Task.WhenAny(startedSource.Task, cancelledSource.Task);
        return completedTask == startedSource.Task;
    }
}