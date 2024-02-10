using MusicPipeBot.Services.Telegram.Core;

namespace MusicPipeBot.Services.Telegram;

public class HostingService(
    IHostApplicationLifetime lifetime,
    IPollingService pollingService,
    ILogger<HostingService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("HostingService is starting...");
        if (!await WaitForAppStartup(lifetime, stoppingToken))
            return;

        while (!stoppingToken.IsCancellationRequested)
            await pollingService.PollAsync(stoppingToken);
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