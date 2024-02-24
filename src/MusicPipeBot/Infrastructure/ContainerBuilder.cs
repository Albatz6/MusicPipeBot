using System.Net;
using System.Threading.RateLimiting;
using Aqueduct.Client;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
using MusicPipeBot.DbContexts;
using MusicPipeBot.Infrastructure.Settings;
using MusicPipeBot.Repositories;
using MusicPipeBot.Services;
using MusicPipeBot.Services.Telegram;
using MusicPipeBot.Services.Telegram.Core;
using MusicPipeBot.StateMachine;
using MusicPipeBot.StateMachine.States;
using OpenTelemetry.Trace;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace MusicPipeBot.Infrastructure;

public static class ContainerBuilder
{
    public static void AddAndConfigureServices(this WebApplicationBuilder builder, AppSettings settings)
    {
        builder.Services.AddHttpClient("telegram_bot_client")
            .AddTypedClient<ITelegramBotClient>((httpClient, _) =>
            {
                TelegramBotClientOptions options = new(settings.TelegramBot.Token);
                return new TelegramBotClient(options, httpClient);
            });
        // builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(settings.TelegramBot.Token));
        builder.Services.AddApiClient(settings);

        builder.Services.AddDbContext<MainDbContext>();
        builder.Services.AddScoped<IUserStatesRepository, UserStatesRepository>();

        builder.Services.AddScoped<IUpdateHandler, UpdateHandler>();
        builder.Services.AddScoped<IReceiverService, ReceiverService>();
        builder.Services.AddScoped<ISendingService, SendingService>();
        builder.Services.AddHostedService<TelegramService>();

        builder.Services.AddScoped<IStateHandler, StateHandler>();
        builder.Services.AddScoped<IState, InitialState>();
        builder.Services.AddScoped<InitialState>();

        builder.Services.AddScoped<IPipeService, PipeService>();
        builder.Services.AddScoped<IDownloadService, DownloadService>();
        builder.Services.AddScoped<IWebhooksService, WebhooksService>();

        builder.Services.AddOpenTelemetry().WithTracing(b => b.AddAspNetCoreInstrumentation().AddConsoleExporter());
        builder.Services.AddRateLimiter(settings.RateLimit);
    }

    private static void AddApiClient(this IServiceCollection services, AppSettings settings)
    {
        var authProvider = new ApiKeyAuthenticationProvider(
            settings.Api.ApiKey,
            settings.Api.AuthHeaderName,
            ApiKeyAuthenticationProvider.KeyLocation.Header);

        var handlers = KiotaClientFactory.CreateDefaultHandlers();
        handlers.Add(new RetryHandler(new RetryHandlerOption
        {
            MaxRetry = settings.HttpClientPolicy.RetriesCount
        }));
        var httpMessageHandler = KiotaClientFactory.ChainHandlersCollectionAndGetFirstLink(
            KiotaClientFactory.GetDefaultHttpMessageHandler(), handlers.ToArray());

        var httpClient = new HttpClient(httpMessageHandler!);
        var requestAdapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient);
        requestAdapter.BaseUrl = settings.Api.BaseUrl;

        services.AddSingleton(new AqueductClient(requestAdapter));
    }

    private static void AddRateLimiter(this IServiceCollection services, RateLimitSettings settings)
    {
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.WindowInSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = settings.QueueLimit,
                        AutoReplenishment = true
                    }));
            options.RejectionStatusCode = (int)HttpStatusCode.TooManyRequests;
        });
    }
}