using Microsoft.AspNetCore.Http;
using Booklify.Domain.Enums;
using System.Text.Json.Serialization;

namespace Booklify.Application.Common.DTOs.Book;

public class CreateBookRequest
{
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
    [JsonPropertyName("is_premium")]
    public bool IsPremium { get; set; } = false;
    [JsonPropertyName("tags")]
    public string? Tags { get; set; }
    [JsonPropertyName("file")]
    public IFormFile File { get; set; } = null!;
} 