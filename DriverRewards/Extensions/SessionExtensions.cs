using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace DriverRewards.Extensions;

public static class SessionExtensions
{
    public static void SetJson<T>(this ISession session, string key, T value)
    {
        session.SetString(key, JsonSerializer.Serialize(value));
    }

    public static T? GetJson<T>(this ISession session, string key)
    {
        var json = session.GetString(key);
        return string.IsNullOrWhiteSpace(json)
            ? default
            : JsonSerializer.Deserialize<T>(json);
    }
}
