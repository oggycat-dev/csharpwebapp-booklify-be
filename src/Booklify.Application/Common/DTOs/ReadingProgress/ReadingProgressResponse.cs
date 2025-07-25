using System.Text.Json.Serialization;

namespace Booklify.Application.Common.DTOs.ReadingProgress;

public class ReadingProgressResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("book_id")]
    public Guid BookId { get; set; }

    [JsonPropertyName("book_title")]
    public string BookTitle { get; set; } = string.Empty;

    [JsonPropertyName("current_chapter_id")]
    public Guid? CurrentChapterId { get; set; }

    [JsonPropertyName("current_chapter_title")]
    public string? CurrentChapterTitle { get; set; }

    [JsonPropertyName("completed_chapter_ids")]
    public List<Guid> CompletedChapterIds { get; set; } = new();

    [JsonPropertyName("accessed_chapter_ids")]
    public List<Guid> AccessedChapterIds { get; set; } = new();

    [JsonPropertyName("completed_chapters_count")]
    public int CompletedChaptersCount { get; set; }

    [JsonPropertyName("total_chapters_count")]
    public int TotalChaptersCount { get; set; }

    [JsonPropertyName("overall_progress_percentage")]
    public double OverallProgressPercentage { get; set; }

    [JsonPropertyName("last_read_at")]
    public DateTime LastReadAt { get; set; }

    [JsonPropertyName("first_read_at")]
    public DateTime? FirstReadAt { get; set; }

    [JsonPropertyName("chapter_progresses")]
    public List<ChapterReadingProgressResponse> ChapterProgresses { get; set; } = new();
}
