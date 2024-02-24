using AqueductCommon.Extensions;
using MusicPipeBot.Services.Telegram.Core;

namespace MusicPipeBot.Services.Telegram;

public class TelegramService(
    IHostApplicationLifetime lifetime,
    IServiceProvider serviceProvider,
    ILogger<TelegramService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.Info("HostingService is starting...");
        if (!await WaitForAppStartup(lifetime, cancellationToken))
            return;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var receiver = scope.ServiceProvider.GetRequiredService<IReceiverService>();

                await receiver.ReceiveAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.Error("Polling failed with exception: {exception}", ex);

                // Cooldown if something goes wrong
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }
    }

    private static async Task<bool> WaitForAppStartup(IHostApplicationLifetime lifetime, CancellationToken cancellationToken)
    {
        var startedSource = new TaskCompletionSource();
        await using var applicationStartedToken = lifetime.ApplicationStarted.Register(() => startedSource.SetResult());

        var cancelledSource = new TaskCompletionSource();
        await using var applicationCancellationToken = cancellationToken.Register(() => cancelledSource.SetResult());

        var completedTask = await Task.WhenAny(startedSource.Task, cancelledSource.Task);
        return completedTask == startedSource.Task;
    }
}