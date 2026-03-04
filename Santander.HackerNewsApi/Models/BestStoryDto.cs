namespace Santander.HackerNewsApi.Models;

/// <summary>
/// Represents a data transfer object containing summary information about a top-rated story, including its title, link,
/// author, posting time, score, and comment count.
/// </summary>
/// <param name="Title">The title of the story as displayed to users. Cannot be null.</param>
/// <param name="Uri">The absolute or relative URI linking to the story's content. Cannot be null.</param>
/// <param name="PostedBy">The username of the author who posted the story. Cannot be null.</param>
/// <param name="Time">The date and time when the story was posted, in UTC.</param>
/// <param name="Score">The score assigned to the story, typically representing its popularity. Must be zero or greater.</param>
/// <param name="CommentCount">The number of comments associated with the story. Must be zero or greater.</param>
public sealed record BestStoryDto(
    string Title,
    string Uri,
    string PostedBy,
    DateTime Time,
    int Score,
    int CommentCount
);
