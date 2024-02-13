using Aqueduct.Client;
using AqueductCommon.Extensions;
using AqueductCommon.Requests.Auth;
using Microsoft.Kiota.Abstractions;

namespace MusicPipeBot.Services;

public interface IWebhooksService
{
    Task<IResult> UpdateYandexSessionId(UpdateYandexSessionIdRequest request);
}

public class WebhooksService(AqueductClient apiClient, ILogger<WebhooksService> logger) : IWebhooksService
{
    public async Task<IResult> UpdateYandexSessionId(UpdateYandexSessionIdRequest request)
    {
        logger.Info("Sending Yandex session id for user with phrase {phrase}", request.Phrase);

        try
        {
            await apiClient.Auth.Yandex.Session.Update.PostAsync(
                new Aqueduct.Client.Models.UpdateYandexSessionIdRequest
                {
                    Phrase = request.Phrase,
                    SessionId = request.SessionId
                });

            return Results.Ok();
        }
        catch (Exception e)
        {
            var apiException = e as ApiException;

            logger.Error("Failed to send Yandex session id for user with phrase {phrase}. Status code: {code}. Message: {message}",
                request.Phrase, apiException?.ResponseStatusCode, e.Message);

            return apiException != null
                ? Results.StatusCode(apiException.ResponseStatusCode)
                : Results.Problem(e.Message);
        }
    }
}