using Santander.HackerNewsApi.Models;

namespace Santander.HackerNewsApi.Services;

/// <summary>
/// Provides methods for retrieving and processing best stories from the Hacker News API.
/// </summary>
public sealed class HackerNewsService : IHackerNewsService
{
    private readonly IHackerNewsClient _client;
    private readonly IConfiguration _config;

    public HackerNewsService(IHackerNewsClient client, IConfiguration config)
    {
        _client = client;
        _config = config;
    }

    public async Task<IReadOnlyList<BestStoryDto>> GetBestStoriesAsync(int n, CancellationToken ct)
    {
        var maxN = _config.GetValue("HackerNews:MaxN", 200);
        n = Math.Clamp(n, 1, Math.Max(1, maxN));

        var ids = await _client.GetBestStoryIdsAsync(ct);

        var firstN = ids.Take(n).ToArray();

        var tasks = firstN.Select(id => _client.GetItemAsync(id, ct)).ToArray();
        var items = await Task.WhenAll(tasks);

        var stories = items
            .Where(i => i is not null && string.Equals(i.Type, "story", StringComparison.OrdinalIgnoreCase))
            .Select(i => new BestStoryDto(
                Title: i.Title ?? string.Empty,
                Uri: i.Url ?? string.Empty,
                PostedBy: i.By ?? string.Empty,
                Time: DateTimeOffset.FromUnixTimeSeconds(i.Time).DateTime, 
                Score: i.Score,
                CommentCount: i.Descendants
            ))
            .OrderByDescending(s => s.Score)
            .ToList();

        return stories;
    }
}
