using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Santander.HackerNewsApi.Caching;
using Santander.HackerNewsApi.Models;

namespace Santander.HackerNewsApi.Services;

/// <summary>
/// Provides an API client for accessing Hacker News stories and items, with built-in caching and concurrency control.
/// </summary>
public sealed class HackerNewsClient : IHackerNewsClient
{
    private const string BestStoriesCacheKey = "beststories:ids";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;
    private readonly IMemoryCache _memory;
    private readonly IDistributedCache _distributed;
    private readonly HackerNewsOptions _options;
    private readonly SemaphoreSlim _itemsSemaphore;

    public HackerNewsClient(
        HttpClient http,
        IMemoryCache memory,
        IDistributedCache distributed,
        IOptions<HackerNewsOptions> options)
    {
        _http = http;
        _memory = memory;
        _distributed = distributed;
        _options = options.Value;

        var maxConcurrency = Math.Max(1, _options.MaxItemFetchConcurrency);
        _itemsSemaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
    }

    public async Task<IReadOnlyList<long>> GetBestStoryIdsAsync(CancellationToken ct)
    {
        // L1 cache
        if (_memory.TryGetValue(BestStoriesCacheKey, out long[]? idsL1) && idsL1 is not null)
            return idsL1;

        // L2 cache
        var idsL2 = await _distributed.GetJsonAsync<long[]>(BestStoriesCacheKey, ct);
        if (idsL2 is not null && idsL2.Length > 0)
        {
            SetL1(BestStoriesCacheKey, idsL2, TimeSpan.FromSeconds(GetIdsTtlSeconds()));
            return idsL2;
        }

        using var resp = await _http.GetAsync("v0/beststories.json", ct);
        resp.EnsureSuccessStatusCode();

        var stream = await resp.Content.ReadAsStreamAsync(ct);
        var ids = await JsonSerializer.DeserializeAsync<long[]>(stream, JsonOptions, ct) ?? Array.Empty<long>();

        var ttl = TimeSpan.FromSeconds(GetIdsTtlSeconds());

        await _distributed.SetJsonAsync(
            BestStoriesCacheKey,
            ids,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            ct);

        SetL1(BestStoriesCacheKey, ids, ttl);

        return ids;
    }

    public async Task<HackerNewsItem?> GetItemAsync(long id, CancellationToken ct)
    {
        var key = $"item:{id}";

        // L1 cache
        if (_memory.TryGetValue(key, out HackerNewsItem? itemL1) && itemL1 is not null)
            return itemL1;

        // L2 cache
        var itemL2 = await _distributed.GetJsonAsync<HackerNewsItem>(key, ct);
        if (itemL2 is not null)
        {
            SetL1(key, itemL2, TimeSpan.FromSeconds(GetItemTtlSeconds()));
            return itemL2;
        }

        await _itemsSemaphore.WaitAsync(ct);
        try
        {
            // double-check on L1 cache after acquiring the semaphore, in case another thread has already fetched and cached the item
            if (_memory.TryGetValue(key, out itemL1) && itemL1 is not null)
                return itemL1;

            itemL2 = await _distributed.GetJsonAsync<HackerNewsItem>(key, ct);
            if (itemL2 is not null)
            {
                SetL1(key, itemL2, TimeSpan.FromSeconds(GetItemTtlSeconds()));
                return itemL2;
            }

            using var resp = await _http.GetAsync($"v0/item/{id}.json", ct);

            if (resp.StatusCode == HttpStatusCode.NotFound)
                return null;

            resp.EnsureSuccessStatusCode();

            var stream = await resp.Content.ReadAsStreamAsync(ct);
            var item = await JsonSerializer.DeserializeAsync<HackerNewsItem>(stream, JsonOptions, ct);

            if (item is null)
                return null;

            var ttl = TimeSpan.FromSeconds(GetItemTtlSeconds());

            await _distributed.SetJsonAsync(
                key,
                item,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
                ct);

            SetL1(key, item, ttl);

            return item;
        }
        finally
        {
            _itemsSemaphore.Release();
        }
    }

    private int GetIdsTtlSeconds() => _options.CacheBestStoriesIdsSeconds;
    private int GetItemTtlSeconds() => _options.CacheItemSeconds;

    private void SetL1<T>(string key, T value, TimeSpan ttl)
        => _memory.Set(key, value, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });
}
