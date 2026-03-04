using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Santander.HackerNewsApi.Models;
using Santander.HackerNewsApi.Services;
using Xunit;

namespace Santander.HackerNewsApi.Tests;

/// <summary>
/// Contains unit tests for the HackerNewsService, verifying its behavior when retrieving and processing best stories
/// from the Hacker News API.
/// </summary>
public class HackerNewsServiceTests
{
    [Fact]
    public async Task GetBestStoriesAsync_Should_Filter_And_Sort_By_Score_Desc()
    {
        // Arrange
        var client = new Mock<IHackerNewsClient>();

        client.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(new long[] { 1, 2, 3 });

        client.Setup(c => c.GetItemAsync(1, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new HackerNewsItem { Id = 1, Type = "story", Title = "A", Url = "u1", By = "x", Time = 10, Score = 10, Descendants = 1 });

        client.Setup(c => c.GetItemAsync(2, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new HackerNewsItem { Id = 2, Type = "comment", Title = "B", Url = "u2", By = "y", Time = 11, Score = 999, Descendants = 99 });

        client.Setup(c => c.GetItemAsync(3, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new HackerNewsItem { Id = 3, Type = "story", Title = "C", Url = "u3", By = "z", Time = 12, Score = 50, Descendants = 7 });

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HackerNews:MaxN"] = "200"
            })
            .Build();

        // Act
        var sut = new HackerNewsService(client.Object, config);
        var result = await sut.GetBestStoriesAsync(3, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Title.Should().Be("C");
        result[1].Title.Should().Be("A");

        result[0].Score.Should().Be(50);
        result[0].CommentCount.Should().Be(7);
        result[0].PostedBy.Should().Be("z");
        result[0].Uri.Should().Be("u3");
        result[0].Time.Should().Be(DateTime.SpecifyKind(result[0].Time, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GetBestStoriesAsync_Should_From_N_To_MaxN()
    {
        // Arrange
        var client = new Mock<IHackerNewsClient>(MockBehavior.Loose);

        client.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(Enumerable.Range(1, 500).Select(x => (long)x).ToArray());

        client.Setup(c => c.GetItemAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((long id, CancellationToken _) =>
                  new HackerNewsItem { Id = id, Type = "story", Title = $"T{id}", Url = $"U{id}", By = "a", Time = 1, Score = (int)id, Descendants = 0 });

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HackerNews:MaxN"] = "200"
            })
            .Build();

        // Act
        var sut = new HackerNewsService(client.Object, config);
        var result = await sut.GetBestStoriesAsync(9999, CancellationToken.None);

        // Assert
        result.Should().HaveCount(200);
        client.Verify(c => c.GetItemAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Exactly(200));
    }
}
