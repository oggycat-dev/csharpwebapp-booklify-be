using System.Text.Json.Serialization;
using Booklify.Domain.Enums;

namespace Booklify.Application.Common.DTOs.Book;

public class BookListItemResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("category_id")]
    public Guid CategoryId { get; set; }

    [JsonPropertyName("category_name")]
    public string CategoryName { get; set; } = string.Empty;

    [JsonPropertyName("approval_status")]
    public ApprovalStatus ApprovalStatus { get; set; }

    [JsonPropertyName("approval_status_string")]
    public string ApprovalStatusString { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public EntityStatus Status { get; set; }

    [JsonPropertyName("status_string")]
    public string StatusString { get; set; } = string.Empty;

    [JsonPropertyName("cover_image_url")]
    public string? CoverImageUrl { get; set; }

    [JsonPropertyName("is_premium")]
    public bool IsPremium { get; set; }

    [JsonPropertyName("average_rating")]
    public double AverageRating { get; set; }

    [JsonPropertyName("total_ratings")]
    public int TotalRatings { get; set; }

    [JsonPropertyName("total_views")]

    public int TotalViews { get; set; }

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("total_chapters")]
    public int TotalChapters { get; set; }

    [JsonPropertyName("published_date")]
    public DateTime? PublishedDate { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }
} 