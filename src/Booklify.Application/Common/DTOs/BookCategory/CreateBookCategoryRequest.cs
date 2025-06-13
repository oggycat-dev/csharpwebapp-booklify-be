using System.Text.Json.Serialization;

namespace Booklify.Application.Common.DTOs.BookCategory;

public class CreateBookCategoryRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
} 