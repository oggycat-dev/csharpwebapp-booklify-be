using System.Text.Json.Serialization;
using Booklify.Domain.Enums;

namespace Booklify.Application.Common.DTOs.Book;

public class BookDetailResponse : BookListItemResponse
{
    [JsonPropertyName("isbn")]
    public string ISBN { get; set; } = string.Empty;

    [JsonPropertyName("publisher")]
    public string Publisher { get; set; } = string.Empty;

    [JsonPropertyName("approval_note")]
    public string? ApprovalNote { get; set; }

    // Note: Status and StatusString are inherited from BookListItemResponse

    [JsonPropertyName("file_path")]
    public string? FilePath { get; set; }

    [JsonPropertyName("file_url")]
    public string? FileUrl { get; set; }

    [JsonPropertyName("page_count")]
    public int PageCount { get; set; }

    [JsonPropertyName("modified_at")]
    public DateTime? ModifiedAt { get; set; }

    [JsonPropertyName("has_chapters")]
    public bool HasChapters { get; set; }

    [JsonPropertyName("tags")]
    public string? Tags { get; set; }

    // ✅ Resume Reading Information - Only for User role, hidden when null for Admin/Staff
    [JsonPropertyName("has_reading_progress")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? HasReadingProgress { get; set; }

    [JsonPropertyName("current_chapter_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? CurrentChapterId { get; set; }

    [JsonPropertyName("current_cfi")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CurrentCfi { get; set; }

    [JsonPropertyName("reading_progress_percentage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? ReadingProgressPercentage { get; set; }

    [JsonPropertyName("completed_chapters_count")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CompletedChaptersCount { get; set; }

    [JsonPropertyName("last_read_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? LastReadAt { get; set; }

    [JsonPropertyName("is_book_completed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsBookCompleted { get; set; }
}
