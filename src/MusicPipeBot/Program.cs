using AqueductCommon.Requests.Auth;
using MusicPipeBot.Infrastructure;
using MusicPipeBot.Infrastructure.Settings;
using MusicPipeBot.Services;

var builder = WebApplication.CreateBuilder(args);
var appSettings = new AppSettings
{
    TelegramBot = new TelegramBotSettings(),
    Api = new ApiSettings(),
    RateLimit = new RateLimitSettings(),
    HttpClientPolicy = new HttpClientPolicySettings()
};
builder.Configuration.GetSection(AppSettings.SectionName).Bind(appSettings);
builder.AddAndConfigureServices(appSettings);

var app = builder.Build();
await app.WaitForDbConnection();
await app.ApplyPendingMigrations();

app.MapPost(
    "/yandex/sessionUpdate",
    async (UpdateYandexSessionIdRequest request, IWebhooksService service) =>
        await service.UpdateYandexSessionId(request));

app.Run();