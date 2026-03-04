namespace Santander.HackerNewsApi.Models;

/// <summary>
/// Configuration options for the HackerNewsClient, including cache durations and concurrency limits.
/// </summary>
public class HackerNewsOptions
{
    public int MaxItemFetchConcurrency { get; set; } = 8;
    public int CacheBestStoriesIdsSeconds { get; set; } = 30;
    public int CacheItemSeconds { get; set; } = 300;
    public int MaxN { get; set; } = 200;
}