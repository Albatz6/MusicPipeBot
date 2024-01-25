using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MusicPipeBot.Infrastructure;
using MusicPipeBot.Infrastructure.Telegram.Core;
using MusicPipeBot.Infrastructure.Telegram.Updaters;
using MusicPipeBot.Services;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace MusicPipeBot;

public static class GenericHostBuilder
{
    public static IHostBuilder GetHostBuilder()
    {
        var hostBuilder = new HostBuilder()
            .ConfigureLogging((hostContext, configLogging) =>
            {
                configLogging.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
                configLogging.AddConsole();
            })
            .ConfigureServices((_, services) =>
            {
                var configFilename = "config.json";
                #if DEBUG
                    configFilename = "config-testing.json";
                #endif

                services.AddSingleton(new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile(configFilename, optional: false));

                services.AddSingleton<ISecretsService, SecretsService>();
                services.AddSingleton<ITelegramBotClient>(sp => sp.GetTelegramBotClient());

                services.AddSingleton<IMessageUpdater, MessageUpdater>();
                services.AddSingleton<IUpdateHandler, UpdateHandlerService>();
                services.AddSingleton<IReceiverService, ReceiverService>();
                services.AddSingleton<IPollingService, PollingService>();
                services.AddSingleton<IPipeService, PipeService>();

                services.AddSingleton<IHostedService, HostingService>();
            });

        return hostBuilder;
    }

    private static ITelegramBotClient GetTelegramBotClient(this IServiceProvider serviceProvider)
    {
        var secretsService = serviceProvider.GetService<ISecretsService>();

        var credentials = secretsService?.GetTelegramCredentials();
        if (credentials is null)
            throw new ApplicationException("No token provided");

        return new TelegramBotClient(credentials.Token);
    }
}