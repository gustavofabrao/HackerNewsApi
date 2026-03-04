using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Santander.HackerNewsApi.Services;
using Santander.HackerNewsApi.Tests.Helpers;
using Xunit;

namespace Santander.HackerNewsApi.Tests;

/// <summary>
/// Contains unit tests for verifying the caching behavior of the HackerNewsClient when retrieving story IDs and items
/// from the Hacker News API.
/// </summary>
public class HackerNewsClientCacheTests
{
    [Fact]
    public async Task GetBestStoryIdsAsync_Should_Use_Cache_And_Avoid_Upstream_On_Second_Call()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(req =>
        {
            if (req.RequestUri!.AbsolutePath.EndsWith("/v0/beststories.json"))
                return FakeHttpMessageHandler.Json("[1,2,3]");

            return FakeHttpMessageHandler.Json("null", System.Net.HttpStatusCode.NotFound);
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://hacker-news.firebaseio.com/") };

        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddDistributedMemoryCache();
        var sp = services.BuildServiceProvider();

        var memory = sp.GetRequiredService<IMemoryCache>();
        var distributed = sp.GetRequiredService<IDistributedCache>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HackerNews:MaxItemFetchConcurrency"] = "8",
                ["HackerNews:Cache:BestStoriesIdsSeconds"] = "30",
                ["HackerNews:Cache:ItemSeconds"] = "300"
            })
            .Build();

        // Act
        var client = new HackerNewsClient(http, memory, distributed, config);

        var ids1 = await client.GetBestStoryIdsAsync(CancellationToken.None);
        var ids2 = await client.GetBestStoryIdsAsync(CancellationToken.None);

        // Assert
        ids1.Should().Equal(1, 2, 3);
        ids2.Should().Equal(1, 2, 3);
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetItemAsync_Should_Cache_And_Avoid_ReFetching()
    {
        // Arrange
        var jsonItem = """
        {
          "id": 10,
          "type": "story",
          "by": "john",
          "time": 100,
          "title": "hello",
          "url": "https://example.com",
          "score": 42,
          "descendants": 5
        }
        """;

        var handler = new FakeHttpMessageHandler(req =>
        {
            if (req.RequestUri!.AbsolutePath.EndsWith("/v0/item/10.json"))
                return FakeHttpMessageHandler.Json(jsonItem);

            return FakeHttpMessageHandler.Json("null", System.Net.HttpStatusCode.NotFound);
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://hacker-news.firebaseio.com/") };

        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddDistributedMemoryCache();
        var sp = services.BuildServiceProvider();

        var memory = sp.GetRequiredService<IMemoryCache>();
        var distributed = sp.GetRequiredService<IDistributedCache>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HackerNews:MaxItemFetchConcurrency"] = "8",
                ["HackerNews:Cache:BestStoriesIdsSeconds"] = "30",
                ["HackerNews:Cache:ItemSeconds"] = "300"
            })
            .Build();

        // Act
        var client = new HackerNewsClient(http, memory, distributed, config);

        var item1 = await client.GetItemAsync(10, CancellationToken.None);
        var item2 = await client.GetItemAsync(10, CancellationToken.None);

        // Assert
        item1.Should().NotBeNull();
        item2.Should().NotBeNull();

        item1!.Score.Should().Be(42);
        item2!.Score.Should().Be(42);

        handler.CallCount.Should().Be(1);
    }
}
