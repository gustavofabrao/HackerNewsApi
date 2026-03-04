using Santander.HackerNewsApi.Models;

namespace Santander.HackerNewsApi.Services;

/// <summary>
/// Defines methods for retrieving and processing best stories from the Hacker News API.
/// </summary>
public interface IHackerNewsService
{
    Task<IReadOnlyList<BestStoryDto>> GetBestStoriesAsync(int n, CancellationToken ct);
}
