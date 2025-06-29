using System.Text.Json.Serialization;

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

    [JsonPropertyName("category_name")]
    public string CategoryName { get; set; } = string.Empty;

    [JsonPropertyName("cover_image_url")]
    public string? CoverImageUrl { get; set; }

    [JsonPropertyName("is_premium")]
    public bool IsPremium { get; set; }

    [JsonPropertyName("has_chapters")]
    public bool HasChapters { get; set; }

    [JsonPropertyName("average_rating")]
    public double AverageRating { get; set; }

    [JsonPropertyName("total_ratings")]
    public int TotalRatings { get; set; }

    [JsonPropertyName("published_date")]
    public DateTime? PublishedDate { get; set; }

    [JsonPropertyName("tags")]
    public string? Tags { get; set; }
} 