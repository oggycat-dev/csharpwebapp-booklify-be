using System.Text.Json.Serialization;

namespace Booklify.Application.Common.DTOs.ReadingProgress;

public class TrackingSessionResponse  
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("book_id")]
    public Guid BookId { get; set; }

    [JsonPropertyName("current_chapter_id")]
    public Guid? CurrentChapterId { get; set; }

    [JsonPropertyName("completed_chapters_count")]
    public int CompletedChaptersCount { get; set; }

    [JsonPropertyName("total_chapters_count")]
    public int TotalChaptersCount { get; set; }

    [JsonPropertyName("overall_progress_percentage")]
    public double OverallProgressPercentage { get; set; }

    [JsonPropertyName("is_completed")]
    public bool IsCompleted { get; set; }

    [JsonPropertyName("last_read_at")]
    public DateTime LastReadAt { get; set; }
} 