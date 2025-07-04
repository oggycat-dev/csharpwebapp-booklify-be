using System.Text.Json.Serialization;

namespace Booklify.Application.Common.DTOs.ReadingProgress;

public class CreateReadingProgressRequest
{
    [JsonPropertyName("book_id")]
    public Guid BookId { get; set; }
    [JsonPropertyName("last_page_number")]
    public int LastPageNumber { get; set; } 
    [JsonPropertyName("current_cfi")]
    public string? CurrentCfi { get; set; }
}
