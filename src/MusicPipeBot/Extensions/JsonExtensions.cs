using System.Text.Json;

namespace MusicPipeBot.Extensions;

public static class JsonExtensions
{
    public static async Task<string> GetJson(this object obj)
    {
        using var stream = new MemoryStream();

        await JsonSerializer.SerializeAsync(stream, obj, obj.GetType());
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}