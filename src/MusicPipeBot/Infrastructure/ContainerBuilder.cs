using MusicPipeBot.Infrastructure.Settings;
using MusicPipeBot.Services;
using MusicPipeBot.Services.Telegram;
using MusicPipeBot.Services.Telegram.Core;
using MusicPipeBot.Services.Telegram.Updaters;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace MusicPipeBot.Infrastructure;

public static class ContainerBuilder
{
    public static void AddAndConfigureServices(this WebApplicationBuilder builder, AppSettings settings)
    {
        builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(settings.TelegramBot.Token));

        builder.Services.AddSingleton<IMessageUpdater, MessageUpdater>();
        builder.Services.AddSingleton<IUpdateHandler, UpdateHandlerService>();
        builder.Services.AddSingleton<IReceiverService, ReceiverService>();
        builder.Services.AddSingleton<IPollingService, PollingService>();

        builder.Services.AddSingleton<IPipeService, PipeService>();
        builder.Services.AddSingleton<IWebhooksService, WebhooksService>();

        builder.Services.AddSingleton<IHostedService, HostingService>();
    }
}