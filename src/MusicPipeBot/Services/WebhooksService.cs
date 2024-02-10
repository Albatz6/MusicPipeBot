using AqueductCommon.Extensions;
using AqueductCommon.Requests.Auth;

namespace MusicPipeBot.Services;

public interface IWebhooksService
{
    Task UpdateYandexSessionId(UpdateYandexSessionIdRequest request);
}

public class WebhooksService(ILogger<WebhooksService> logger) : IWebhooksService
{
    public async Task UpdateYandexSessionId(UpdateYandexSessionIdRequest request)
    {
        logger.Info("Got Yandex session id");
    }
}