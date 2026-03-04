using System.Text.Json.Serialization;

namespace Santander.HackerNewsApi.Models;

/// <summary>
/// Represents an item retrieved from the Hacker News API, such as a story, comment, poll, or job posting.
/// </summary>
public sealed class HackerNewsItem
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("by")]
    public string? By { get; init; }

    // Unix time (seconds)
    [JsonPropertyName("time")]
    public long Time { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("score")]
    public int Score { get; init; }

    // Total comment count
    [JsonPropertyName("descendants")]
    public int Descendants { get; init; }
}
