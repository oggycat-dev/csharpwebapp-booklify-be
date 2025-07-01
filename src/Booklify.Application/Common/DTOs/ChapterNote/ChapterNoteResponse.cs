using System.Text.Json.Serialization;

namespace Booklify.Application.Common.DTOs.ChapterNote;

/// <summary>
/// Detailed chapter note response with all fields
/// </summary>
public class ChapterNoteResponse : ChapterNoteListItemResponse
{
    
    [JsonPropertyName("highlighted_text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HighlightedText { get; set; }
    
    [JsonPropertyName("color")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Color { get; set; }
    
    // Chapter info
    [JsonPropertyName("chapter_title")]
    public string ChapterTitle { get; set; } = string.Empty;
    
    [JsonPropertyName("chapter_order")]
    public int ChapterOrder { get; set; }

    // Book info
    [JsonPropertyName("book_id")]
    public Guid BookId { get; set; }
    
    [JsonPropertyName("book_title")]
    public string BookTitle { get; set; } = string.Empty;
} 