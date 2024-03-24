namespace MusicPipeBot.Helpers;

public static class QueryValidationHelper
{
    public static string? GetUrlFromQuery(string? query)
    {
        var validatedQuery = ValidateQuery(query);
        if (validatedQuery == default)
            return null;

        if (validatedQuery.Contains("youtu.be"))
            return GetYoutubeUrl(validatedQuery);

        // These are the markers of track link for Spotify and YTMusic
        if (!validatedQuery.Contains("track") && !validatedQuery.Contains("watch"))
            return null;

        var isValidUri = Uri.TryCreate(validatedQuery, UriKind.Absolute, out var uriResult)
                         && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        return !isValidUri ? null : validatedQuery;
    }

    private static string? ValidateQuery(string? query)
    {
        if (query is null)
            return null;

        // Separate by ; and & in case somebody tries injecting other commands into the query
        var keywords = query.Split(' ', ';', '&');
        if (keywords.Length <= 1)
            return null;

        return string.IsNullOrWhiteSpace(keywords[1]) ? null : keywords[1];
    }

    private static string? GetYoutubeUrl(string validatedQuery)
    {
        var ytHashWithQueryParams = validatedQuery.Split('/').LastOrDefault();
        if (ytHashWithQueryParams == default)
            return null;

        // Avoiding all those analytical query params like "si"
        var ytHash = ytHashWithQueryParams.Split('?').FirstOrDefault();
        return ytHash == default ? null : $"https://music.youtube.com/watch?v={ytHash}";
    }
}