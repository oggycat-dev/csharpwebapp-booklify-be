using System.Text.Json.Serialization;

namespace Booklify.Application.Common.DTOs.ReadingProgress;

public class ChapterReadingProgressResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("reading_progress_id")]
    public Guid ReadingProgressId { get; set; }

    [JsonPropertyName("chapter_id")]
    public Guid ChapterId { get; set; }

    [JsonPropertyName("chapter_title")]
    public string ChapterTitle { get; set; } = string.Empty;

    [JsonPropertyName("chapter_order")]
    public int ChapterOrder { get; set; }

    [JsonPropertyName("current_cfi")]
    public string? CurrentCfi { get; set; }

    [JsonPropertyName("is_completed")]
    public bool IsCompleted { get; set; }

    [JsonPropertyName("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [JsonPropertyName("last_read_at")]
    public DateTime LastReadAt { get; set; }
} 