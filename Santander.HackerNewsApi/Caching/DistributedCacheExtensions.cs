using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Santander.HackerNewsApi.Caching;

/// <summary>
/// Provides extension methods for storing and retrieving JSON-serialized objects in an implementation of <see cref="IDistributedCache"/>.
/// </summary>
public static class DistributedCacheExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<T?> GetJsonAsync<T>(this IDistributedCache cache, string key, CancellationToken ct)
    {
        var bytes = await cache.GetAsync(key, ct);
        if (bytes is null || bytes.Length == 0) return default;

        return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
    }

    public static async Task SetJsonAsync<T>(
        this IDistributedCache cache,
        string key,
        T value,
        DistributedCacheEntryOptions options,
        CancellationToken ct)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        await cache.SetAsync(key, bytes, options, ct);
    }
}
