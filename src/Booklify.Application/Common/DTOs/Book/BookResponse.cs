using System.Text.Json.Serialization;
using Booklify.Application.Common.DTOs.BookCategory;

namespace Booklify.Application.Common.DTOs.Book;

public class BookResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;
    [JsonPropertyName("isbn")]
    public string ISBN { get; set; } = string.Empty;
    [JsonPropertyName("publisher")]
    public string Publisher { get; set; } = string.Empty;
    [JsonPropertyName("category_id")]
    public Guid CategoryId { get; set; }
    [JsonPropertyName("category_name")]
    public string CategoryName { get; set; } = string.Empty;
    [JsonPropertyName("approval_status")]
    public string ApprovalStatus { get; set; } = string.Empty;
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    [JsonPropertyName("cover_image_url")]
    public string? CoverImageUrl { get; set; }
    [JsonPropertyName("is_premium")]
    public bool IsPremium { get; set; }
    [JsonPropertyName("file_path")]
    public string? FilePath { get; set; }
    [JsonPropertyName("file_url")]
    public string? FileUrl { get; set; }
    [JsonPropertyName("tags")]
    public string? Tags { get; set; }
    [JsonPropertyName("page_count")]
    public int PageCount { get; set; }
    [JsonPropertyName("created_at")]
    public DateTime? ModifiedAt { get; set; }
    [JsonPropertyName("has_chapters")]
    public bool HasChapters { get; set; }
    
    
} 