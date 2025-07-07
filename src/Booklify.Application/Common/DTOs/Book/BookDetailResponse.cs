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
}
