using System.Text.Json.Serialization;
using Booklify.Application.Common.DTOs.BookCategory;

namespace Booklify.Application.Common.DTOs.Book;

public class BookResponse : BookListItemResponse
{
    [JsonPropertyName("isbn")]
    public string ISBN { get; set; } = string.Empty;

    [JsonPropertyName("publisher")]
    public string Publisher { get; set; } = string.Empty;

    [JsonPropertyName("category_id")]
    public Guid CategoryId { get; set; }

    [JsonPropertyName("approval_status")]
    public string ApprovalStatus { get; set; } = string.Empty;

    [JsonPropertyName("approval_note")]
    public string? ApprovalNote { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("file_path")]
    public string? FilePath { get; set; }

    [JsonPropertyName("file_url")]
    public string? FileUrl { get; set; }

    [JsonPropertyName("page_count")]
    public int PageCount { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("modified_at")]
    public DateTime? ModifiedAt { get; set; }

    [JsonPropertyName("chapters")]
    public List<ChapterResponse>? Chapters { get; set; }

    [JsonPropertyName("is_chapters_limited")]
    public bool IsChaptersLimited { get; set; }

    public double AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public int TotalViews { get; set; }
    public DateTime? PublishedDate { get; set; }
} 