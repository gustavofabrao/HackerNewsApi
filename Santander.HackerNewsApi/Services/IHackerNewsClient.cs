using Santander.HackerNewsApi.Models;

namespace Santander.HackerNewsApi.Services;

/// <summary>
/// Defines methods for retrieving stories and items from the Hacker News API asynchronously.
/// </summary>
public interface IHackerNewsClient
{
    Task<IReadOnlyList<long>> GetBestStoryIdsAsync(CancellationToken ct);
    Task<HackerNewsItem?> GetItemAsync(long id, CancellationToken ct);
}
