using System.Text.Json.Serialization;
using Booklify.Domain.Enums;

namespace Booklify.Application.Common.DTOs.ChapterNote;

public class CreateChapterNoteRequest
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
    
    [JsonPropertyName("page_number")]
    public int PageNumber { get; set; }
    
    [JsonPropertyName("cfi")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Cfi { get; set; }
    
    [JsonPropertyName("cfi_start")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CfiStart { get; set; }
    [JsonPropertyName("cfi_end")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CfiEnd { get; set; }
    
    [JsonPropertyName("highlighted_text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HighlightedText { get; set; }
    
    [JsonPropertyName("color")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Color { get; set; }
    
    [JsonPropertyName("note_type")]
    public ChapterNoteType NoteType { get; set; } = ChapterNoteType.TextNote;
    
    [JsonPropertyName("chapter_id")]
    public Guid ChapterId { get; set; }
} 